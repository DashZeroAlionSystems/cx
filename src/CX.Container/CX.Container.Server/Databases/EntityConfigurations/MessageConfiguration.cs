namespace CX.Container.Server.Databases.EntityConfigurations;

using CX.Container.Server.Domain.Messages;
using CX.Container.Server.Domain.FeedbackTypes;
using CX.Container.Server.Domain.MessageTypes;
using CX.Container.Server.Domain.ContentTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    /// <summary>
    /// The database configuration for Messages. 
    /// </summary>
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        // Relationship Marker -- Deleting or modifying this comment could cause incomplete relationship scaffolding
        builder.HasOne(m => m.Thread)
                    .WithMany(t => t.Messages)
                    .HasForeignKey(t => t.ThreadId);

        builder.HasMany(m => m.Citations)
                    .WithOne(t => t.Message)
                    .HasForeignKey(t => t.MessageId);

        // Property Marker -- Deleting or modifying this comment could cause incomplete relationship scaffolding

        builder.Property(x => x.Feedback)
            .HasConversion(x => x.Value, x => FeedbackType.Of(x));

        builder.Property(x => x.MessageType)
            .HasConversion(x => x.Value, x => MessageType.Of(x));

        builder.Property(x => x.ContentType)
            .HasConversion(x => x.Value, x => ContentType.Of(x));
        
        builder.HasIndex(x => new {x.ThreadId, x.CreatedOn, x.IsDeleted})
            .HasDatabaseName("IX_Messages_Thread_CreatedOn_IsDeleted");

        builder.HasIndex(x => new { x.Id, x.IsFlagged })
            .HasDatabaseName("IX_Messages_Id_IsFlagged");


    }
}
