namespace CX.Engine.DocExtractors.Images;

public class PDFToJpgOptions
{
    public string ScriptPath { get; set; } = null!;
    public string BinaryImageStore { get; set; } = null!;
    public string JsonDocumentStore { get; set; } = null!;
    public string PythonProcess { get; set; } = null!;
    public string PopplerPath { get; set; } = null!;
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PythonProcess))
            throw new ArgumentException($"{nameof(PDFToJpgOptions)}.{nameof(PythonProcess)} is required");

        if (string.IsNullOrWhiteSpace(ScriptPath))
            throw new ArgumentException($"{nameof(PDFToJpgOptions)}.{nameof(ScriptPath)} is required");

        if (string.IsNullOrWhiteSpace(BinaryImageStore))
            throw new InvalidOperationException($"Missing {nameof(PDFToJpgOptions)}.{nameof(BinaryImageStore)}");
        
        if (string.IsNullOrWhiteSpace(JsonDocumentStore))
            throw new InvalidOperationException($"Missing {nameof(PDFToJpgOptions)}.{nameof(JsonDocumentStore)}");

        if (string.IsNullOrWhiteSpace(PopplerPath))
            throw new InvalidOperationException($"Missing {nameof(PDFToJpgOptions)}.{nameof(PopplerPath)}");
    }
}