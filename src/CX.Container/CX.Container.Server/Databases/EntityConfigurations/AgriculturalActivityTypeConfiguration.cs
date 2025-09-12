namespace CX.Container.Server.Databases.EntityConfigurations;

using CX.Container.Server.Domain.AgriculturalActivityTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class AgriculturalActivityTypeConfiguration : IEntityTypeConfiguration<AgriculturalActivityType>
{
    /// <summary>
    /// The database configuration for AgriculturalActivityTypes. 
    /// </summary>
    public void Configure(EntityTypeBuilder<AgriculturalActivityType> builder)
    {
        // Relationship Marker -- Deleting or modifying this comment could cause incomplete relationship scaffolding

        // Property Marker -- Deleting or modifying this comment could cause incomplete relationship scaffolding
        
    }
}