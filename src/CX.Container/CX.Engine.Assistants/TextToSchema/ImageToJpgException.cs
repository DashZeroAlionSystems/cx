namespace CX.Engine.Assistants.TextToSchema;

public class ImageToJpgException : Exception
{
    public ImageToJpgException() : base("Failed to convert image to jpeg.")
    {
    }
}