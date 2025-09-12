namespace CX.Container.Server.Databases.EntityConfigurations;

using CX.Container.Server.Domain.Sources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class SourceConfiguration : IEntityTypeConfiguration<Source>
{
    /// <summary>
    /// The database configuration for Sources. 
    /// </summary>
    public void Configure(EntityTypeBuilder<Source> builder)
    {
        // Relationship Marker -- Deleting or modifying this comment could cause incomplete relationship scaffolding

        // Property Marker -- Deleting or modifying this comment could cause incomplete relationship scaffolding
        
    }
}
