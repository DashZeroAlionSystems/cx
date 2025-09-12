namespace CX.Container.Server.Databases;

using CX.Container.Server.Databases.EntityConfigurations;
using CX.Container.Server.Domain;
using CX.Container.Server.Domain.AgriculturalActivities;
using CX.Container.Server.Domain.AgriculturalActivityTypes;
using CX.Container.Server.Domain.Citations;
using CX.Container.Server.Domain.Countries;
using CX.Container.Server.Domain.MessageCitations;
using CX.Container.Server.Domain.Messages;
using CX.Container.Server.Domain.Nodes;
using CX.Container.Server.Domain.Preferences;
using CX.Container.Server.Domain.Profiles;
using CX.Container.Server.Domain.Projects;
using CX.Container.Server.Domain.RolePermissions;
using CX.Container.Server.Domain.SourceDocuments;
using CX.Container.Server.Domain.Sources;
using CX.Container.Server.Domain.UiLogs;
using CX.Container.Server.Domain.Users;
using CX.Container.Server.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

public sealed class AelaDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;
    private readonly TimeProvider _clock;

    public AelaDbContext(
        DbContextOptions<AelaDbContext> options,
        ICurrentUserService currentUserService, 
        IMediator mediator,
        TimeProvider clock) : base(options)
    {
        _currentUserService = currentUserService;
        _mediator = mediator;
        _clock = clock;
    }

    #region DbSet Region - Do Not Delete    
    public DbSet<Node> Nodes { get; set; }
    public DbSet<UiLogs> UiLogs { get; set; }
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<AgriculturalActivity> AgriculturalActivities { get; set; }
    public DbSet<AgriculturalActivityType> AgriculturalActivityTypes { get; set; }
    public DbSet<Country> Countries { get; set; }
    public DbSet<Source> Sources { get; set; }
    public DbSet<SourceDocument> SourceDocuments { get; set; }
    public DbSet<Domain.Threads.Thread> Threads { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<MessageCitation> MessageCitations { get; set; }
    public DbSet<Citation> Citations { get; set; }
    public DbSet<Preference> Preferences { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<User> Users { get; set; }
    
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<Project> Projects { get; set; }
    #endregion DbSet Region - Do Not Delete

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.FilterSoftDeletedRecords();
        /* any query filters added after this will override soft delete 
                https://docs.microsoft.com/en-us/ef/core/querying/filters
                https://github.com/dotnet/efcore/issues/10275
        */

        #region Entity Database Config Region - Only delete if you don't want to automatically add configurations                                
        modelBuilder.ApplyConfiguration(new UiLogsConfiguration());
        modelBuilder.ApplyConfiguration(new ProfileConfiguration());
        modelBuilder.ApplyConfiguration(new AgriculturalActivityConfiguration());
        modelBuilder.ApplyConfiguration(new AgriculturalActivityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new CountryConfiguration());
        modelBuilder.ApplyConfiguration(new SourceConfiguration());
        modelBuilder.ApplyConfiguration(new SourceDocumentConfiguration());
        modelBuilder.ApplyConfiguration(new ThreadConfiguration());
        modelBuilder.ApplyConfiguration(new MessageConfiguration());
        modelBuilder.ApplyConfiguration(new PreferenceConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new RolePermissionConfiguration());

        #endregion Entity Database Config Region - Only delete if you don't want to automatically add configurations
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        QueueDomainEvents();
        var result = base.SaveChanges();
        DispatchDomainEvents().GetAwaiter().GetResult();
        return result;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        UpdateAuditFields();
        QueueDomainEvents();
        var result = await base.SaveChangesAsync(cancellationToken);
        await DispatchDomainEvents();
        return result;
    }

    
    private readonly List<DomainEvent> _domainEvents = [];
    private void QueueDomainEvents()
    {
        var entities = ChangeTracker.Entries<IDomainEvent>()
            .Select(po => po.Entity)
            .Where(po => po.DomainEvents.Count != 0)
            .ToArray();

        foreach (var entity in entities)
        {
            _domainEvents.AddRange(entity.DomainEvents);
            entity.DomainEvents.Clear();
        }
    }
    
    public void QueueDomainEvent(DomainEvent @event)
    {
        _domainEvents.Add(@event);
    }
    
    public async Task WrapInTransactionAsync(Func<AelaDbContext, CancellationToken, Task> dbUpdates, CancellationToken cancellationToken = default)
    {
        await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            await dbUpdates(this, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await DispatchDomainEvents();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
    
    private async Task DispatchDomainEvents()
    {
        foreach (var @event in _domainEvents)
            await _mediator.Publish(@event);

        _domainEvents.Clear();
    }
        
    private void UpdateAuditFields()
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.UpdateCreationProperties(now, _currentUserService?.UserId);
                    entry.Entity.UpdateModifiedProperties(now, _currentUserService?.UserId);
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdateModifiedProperties(now, _currentUserService?.UserId);
                    break;
                
                case EntityState.Deleted:
                    if (entry.Entity is IHardDelete) break;
                    
                    entry.State = EntityState.Modified;
                    entry.Entity.UpdateModifiedProperties(now, _currentUserService?.UserId);
                    entry.Entity.UpdateIsDeleted(true);
                    break;
            }
        }
    }
}

public class region
{
}

public static class Extensions
{
    public static void FilterSoftDeletedRecords(this ModelBuilder modelBuilder)
    {
        Expression<Func<IAuditable, bool>> filterExpr = e => !e.IsDeleted;
        
        foreach (var mutableEntityType in modelBuilder.Model.GetEntityTypes()
            .Where(m => m.ClrType.IsAssignableTo(typeof(IAuditable))))
        {
            // modify expression to handle correct child type
            var parameter = Expression.Parameter(mutableEntityType.ClrType);
            var body = ReplacingExpressionVisitor
                .Replace(filterExpr.Parameters[0], parameter, filterExpr.Body);
            var lambdaExpression = Expression.Lambda(body, parameter);

            // set filter
            mutableEntityType.SetQueryFilter(lambdaExpression);
        }
    }
}
