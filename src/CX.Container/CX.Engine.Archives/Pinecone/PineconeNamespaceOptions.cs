namespace CX.Engine.Archives.Pinecone;

public class PineconeNamespaceOptions : IValidatable
{
    public string PineconeArchive { get; set; }
    public string Namespace { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PineconeArchive))
            throw new InvalidOperationException($"{nameof(PineconeArchive)} is required");

        if (Namespace == null)
            throw new InvalidOperationException($"{nameof(Namespace)} is required");

        if (Namespace.Trim() != "" && !StringValidators.IsValidAlphaNumericUnderscoreStartingWithLetter(Namespace))
            throw new InvalidOperationException("Namespace must contain only alphanumeric characters or underscores and start with a letter or underscore");
    }
}