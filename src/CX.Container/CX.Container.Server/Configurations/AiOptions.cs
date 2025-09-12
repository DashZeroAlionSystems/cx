namespace CX.Container.Server.Configurations;

public class AiOptions
{
    public bool UseVectorLinkImporter { get; set; }
    public bool UseVectorLinkDocumentExtractors { get; set; }
    public string ChannelName { get; set; }
    public string ChatUrl { get; set; } = String.Empty;
    public string ChatApiKey { get; set; } = String.Empty;
    public string TrainApiKey { get; set; } = String.Empty;
    public string ProcessDocumentUrl { get; set; } = String.Empty;
    public string ProcessDocumentStatusUrl { get; set; } = String.Empty;
    public string DocumentKey { get; set; } = String.Empty;
    public string DecoratorUrl { get; set; } = String.Empty;
    public string DecoratorStatusUrl { get; set; } = String.Empty;
    public string TrainUrl { get; set; } = String.Empty;
    public string DeleteUrl { get; set; } = String.Empty;
    public int HttpTimeoutInSeconds { get; set; } = 60;
    public string TrainNamespace { get; set; }
    public string TrainIndexName { get; set; }

    public string AiServerUrl { get; set; } = "http://ai-server:8000";

    public bool AutoProcess { get; set; } = false;

    public string DecoratorUrlTaskId(string taskId) { 
        return $"{DecoratorStatusUrl}/{taskId}/".Replace("train-status//", "train-status/");
     }
    public string ProcessDocumentStatusUrlTaskId(string taskId)
    {
        return $"{ProcessDocumentStatusUrl}/{taskId}/".Replace("status//", "status/");
    }
    public string DeleteNamespace()
    {
        return $"{DeleteUrl}".Replace("file/", "");
    }
}