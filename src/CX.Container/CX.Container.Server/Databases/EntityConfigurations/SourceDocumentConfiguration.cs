namespace CX.Container.Server.Databases.EntityConfigurations;

using CX.Container.Server.Domain.SourceDocuments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class SourceDocumentConfiguration : IEntityTypeConfiguration<SourceDocument>
{
    /// <summary>
    /// The database configuration for SourceDocuments. 
    /// </summary>
    public void Configure(EntityTypeBuilder<SourceDocument> builder)
    {
        // Relationship Marker -- Deleting or modifying this comment could cause incomplete relationship scaffolding
        builder.HasOne(d => d.Source)
            .WithMany(s => s.SourceDocuments)
            .HasForeignKey(d => d.SourceId);
        
        // Property Marker -- Deleting or modifying this comment could cause incomplete relationship scaffolding

        builder.ComplexProperty(x => x.Status,
            pb => pb.Property(y => y.Value)
                .HasColumnName("status"));

        // builder.ComplexProperty(x => x.DocumentSourceType,
        //     pb => pb.Property(y => y.Value)
        //         .HasColumnName("document_source_type"));

        builder.HasMany(m => m.Citations)
                   .WithOne(t => t.SourceDocument)
                   .HasForeignKey(t => t.SourceDocumentId)
                   .OnDelete(DeleteBehavior.Cascade);


        builder.OwnsOne(x => x.DocumentSourceType, opts =>
        {
            opts.Property(x => x.Value).HasColumnName("document_source_type");

        }).Navigation(x => x.DocumentSourceType);
    }
}
