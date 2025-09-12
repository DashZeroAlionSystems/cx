namespace CX.Container.Server.Databases.EntityConfigurations;

using CX.Container.Server.Domain.Citations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class CitationConfiguration : IEntityTypeConfiguration<Citation>
{
    /// <summary>
    /// The database configuration for Citations. 
    /// </summary>
    public void Configure(EntityTypeBuilder<Citation> builder)
    {
        // Relationship Marker -- Deleting or modifying this comment could cause incomplete relationship scaffolding
        builder.HasOne(s => s.SourceDocument)
                    .WithMany(c => c.Citations)
                    .HasForeignKey(c => c.SourceDocumentId);         
    }
}
