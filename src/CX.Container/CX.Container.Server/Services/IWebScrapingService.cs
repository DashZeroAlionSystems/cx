namespace CX.Container.Server.Services;

public interface IWebScrapingService
{    
    Task<IFormFile> GeneratePdfFromUrlAsync(string url);
}