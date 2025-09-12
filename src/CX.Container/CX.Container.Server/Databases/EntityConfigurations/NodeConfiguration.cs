namespace CX.Container.Server.Databases.EntityConfigurations;

using CX.Container.Server.Domain.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class NodeConfiguration : IEntityTypeConfiguration<Node>
{
    /// <summary>
    /// The database configuration for Nodes. 
    /// </summary>
    public void Configure(EntityTypeBuilder<Node> builder)
    {
        // Relationship Marker -- Deleting or modifying this comment could cause incomplete relationship scaffolding
        builder.HasOne(x => x.Parent)
        .WithMany(x => x.Nodes)
        .HasForeignKey(x => x.ParentId) // Specify the foreign key property
        .OnDelete(DeleteBehavior.Restrict); // Or specify the desired delete behavior

        builder.HasOne(x => x.Project)
            .WithMany(x => x.Nodes)
            .HasForeignKey(x => x.ProjectId); // Specify the foreign key property

        // Property Marker -- Deleting or modifying this comment could cause incomplete relationship scaffolding

    }
}
