namespace CX.Container.Server.Domain.UiLogs;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Destructurama.Attributed;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain.UiLogs.Models;
using CX.Container.Server.Domain.UiLogs.DomainEvents;


public class UiLogs : Entity<Guid>
{
    public string From { get; private set; }

    public Guid SourceDocumentId { get; private set; }

    public string Details { get; private set; }

    // Add Props Marker -- Deleting this comment will cause the add props utility to be incomplete


    public static UiLogs Create(UiLogsForCreation uiLogsForCreation)
    {
        var newUiLogs = new UiLogs();

        newUiLogs.From = uiLogsForCreation.From;
        newUiLogs.Details = uiLogsForCreation.Details;

        newUiLogs.QueueDomainEvent(new UiLogsCreated(){ UiLogs = newUiLogs });
        
        return newUiLogs;
    }

    public UiLogs Update(UiLogsForUpdate uiLogsForUpdate)
    {
        From = uiLogsForUpdate.From;
        Details = uiLogsForUpdate.Details;

        QueueDomainEvent(new UiLogsUpdated(){ Id = Id });
        return this;
    }

    // Add Prop Methods Marker -- Deleting this comment will cause the add props utility to be incomplete
    
    protected UiLogs() { } // For EF + Mocking
}