namespace CX.Container.Server.Databases.EntityConfigurations;

using CX.Container.Server.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    /// <summary>
    /// The database configuration for Profiles. 
    /// </summary>
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        // Relationship Marker -- Deleting or modifying this comment could cause incomplete relationship scaffolding
        builder
            .HasMany(x => x.AgriculturalActivities)
            .WithOne(x => x.Profile)
            .HasForeignKey(x => x.ProfileId);
        builder
            .HasOne(x => x.Country)
            .WithMany(x => x.Profiles)
            .HasForeignKey(x => x.CountryId);

        // Property Marker -- Deleting or modifying this comment could cause incomplete relationship scaffolding


    }
}
