namespace CX.Container.Server.Databases.EntityConfigurations;

using CX.Container.Server.Domain.Preferences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class PreferenceConfiguration : IEntityTypeConfiguration<Preference>
{
    /// <summary>
    /// The database configuration for Preferences. 
    /// </summary>
    public void Configure(EntityTypeBuilder<Preference> builder)
    {
        // Relationship Marker -- Deleting or modifying this comment could cause incomplete relationship scaffolding

        // Property Marker -- Deleting or modifying this comment could cause incomplete relationship scaffolding
        

    }
}