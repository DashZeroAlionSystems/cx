namespace CX.Engine.Assistants.ArtifactAssists;

public class ArtifactException : Exception
{
    public ArtifactException(string message) : base(message) 
    { 
    }

    public ArtifactException(string message, Exception innerException) : base(message, innerException)
    {
    }
}