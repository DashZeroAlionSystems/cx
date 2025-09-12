namespace CX.Container.Server.Databases.EntityConfigurations;

using CX.Container.Server.Domain.AgriculturalActivities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class AgriculturalActivityConfiguration : IEntityTypeConfiguration<AgriculturalActivity>
{
    /// <summary>
    /// The database configuration for AgriculturalActivities. 
    /// </summary>
    public void Configure(EntityTypeBuilder<AgriculturalActivity> builder)
    {
        // Relationship Marker -- Deleting or modifying this comment could cause incomplete relationship scaffolding
        builder
            .HasOne(x => x.AgriculturalActivityType)
            .WithMany(x => x.AgriculturalActivities)
            .HasForeignKey(x => x.AgriculturalActivityTypeId);
        
        builder.HasOne(x => x.Profile)
            .WithMany(x => x.AgriculturalActivities)
            .HasForeignKey(x => x.ProfileId);

        // Property Marker -- Deleting or modifying this comment could cause incomplete relationship scaffolding
        
    }
}
