namespace CX.Container.Server.Databases.EntityConfigurations;

using CX.Container.Server.Domain.MessageCitations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class MessageCitationConfiguration : IEntityTypeConfiguration<MessageCitation>
{
    /// <summary>
    /// The database configuration for MessageCitations. 
    /// </summary>
    public void Configure(EntityTypeBuilder<MessageCitation> builder)
    {
        // Relationship Marker -- Deleting or modifying this comment could cause incomplete relationship scaffolding
        builder.HasOne(m => m.Message)
                    .WithMany(t => t.Citations)
                    .HasForeignKey(t => t.MessageId);         
    }
}
