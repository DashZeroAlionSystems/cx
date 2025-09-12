namespace CX.Container.Server.Databases;

using EntityConfigurations;
using Domain.RolePermissions;
using Domain.Users;
using Domain.Profiles;
using Domain.AgriculturalActivities;
using Domain.AgriculturalActivityTypes;
using Domain.Countries;
using Domain.Sources;
using Domain.SourceDocuments;
using Domain.Messages;
using Domain.Preferences;
using Domain.UiLogs;
using Microsoft.EntityFrameworkCore;

public sealed class AelaDbReadContext : DbContext
{
    public AelaDbReadContext(
        DbContextOptions<AelaDbReadContext> options) : base(options)
    {
    }

    #region DbSet Region - Do Not Delete
    public DbSet<UiLogs> UiLogs { get; set; }
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<AgriculturalActivity> AgriculturalActivities { get; set; }
    public DbSet<AgriculturalActivityType> AgriculturalActivityTypes { get; set; }
    public DbSet<Country> Countries { get; set; }
    public DbSet<Source> Sources { get; set; }
    public DbSet<SourceDocument> SourceDocuments { get; set; }
    public DbSet<Domain.Threads.Thread> Threads { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Preference> Preferences { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
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
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
}
