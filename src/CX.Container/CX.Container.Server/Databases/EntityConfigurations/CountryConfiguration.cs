namespace CX.Container.Server.Databases.EntityConfigurations;

using CX.Container.Server.Domain.Countries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    /// <summary>
    /// The database configuration for Countries. 
    /// </summary>
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        // Relationship Marker -- Deleting or modifying this comment could cause incomplete relationship scaffolding

        // Property Marker -- Deleting or modifying this comment could cause incomplete relationship scaffolding
        

    }
}