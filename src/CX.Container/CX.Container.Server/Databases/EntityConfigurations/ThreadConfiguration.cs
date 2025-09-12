namespace CX.Container.Server.Databases.EntityConfigurations;

using CX.Container.Server.Domain.Threads;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class ThreadConfiguration : IEntityTypeConfiguration<Thread>
{
    /// <summary>
    /// The database configuration for Threads. 
    /// </summary>
    public void Configure(EntityTypeBuilder<Thread> builder)
    {
        // Relationship Marker -- Deleting or modifying this comment could cause incomplete relationship scaffolding
        builder.HasMany(t => t.Messages)
            .WithOne(m => m.Thread)
            .HasForeignKey(m => m.ThreadId);

        // Property Marker -- Deleting or modifying this comment could cause incomplete relationship scaffolding
        builder
            .HasIndex(t => new {t.CreatedBy, t.CreatedOn, t.IsDeleted})
            .HasDatabaseName("IX_Threads_CreatedBy_CreatedOn_IsDeleted");
    }
}
