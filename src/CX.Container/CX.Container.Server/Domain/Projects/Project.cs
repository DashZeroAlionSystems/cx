namespace CX.Container.Server.Domain.Projects;

using CX.Container.Server.Domain.Projects.Models;
using CX.Container.Server.Domain.Nodes;
using CX.Container.Server.Domain.Projects.DomainEvents;
using Microsoft.IdentityModel.Tokens;

public class Project : Entity<Guid>
{
    public string Name { get; private set; }

    public string Description { get; private set; }

    public string Thumbnail { get; private set; }

    public string Namespace { get; private set; }

    public IReadOnlyCollection<Node> Nodes { get; } = new List<Node>();


    // Add Props Marker -- Deleting this comment will cause the add props utility to be incomplete


    public static Project Create(ProjectForCreation projectForCreation)
    {
        var newProject = new Project
        {
            Name = projectForCreation.Name,
            Description = projectForCreation.Description,
            Thumbnail = projectForCreation.Thumbnail
        };

        newProject.QueueDomainEvent(new ProjectCreated(){ Project = newProject });

        return newProject;
    }

    public Project Update(ProjectForUpdate projectForUpdate)
    {
        if (!projectForUpdate.Name.IsNullOrEmpty()) Name = projectForUpdate.Name;

        Description = projectForUpdate.Description;

        Thumbnail = projectForUpdate.Thumbnail;

        QueueDomainEvent(new ProjectUpdated(){ Id = Id });
        return this;
    }

    // Add Prop Methods Marker -- Deleting this comment will cause the add props utility to be incomplete

    protected Project() { } // For EF + Mocking
}
