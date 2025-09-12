using PuppeteerSharp;

namespace CX.Container.Server.Services;

/// <summary>
/// Provides web scraping services.
/// </summary>
public class WebScrapingService : IWebScrapingService
{
    private readonly BrowserFetcher _browserFetcher;
    private readonly LaunchOptions _launchOptions;
    private readonly ILogger<WebScrapingService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebScrapingService"/> class.
    /// </summary>
    public WebScrapingService(ILogger<WebScrapingService> logger)
    {
        _browserFetcher = new BrowserFetcher();
        _launchOptions = new LaunchOptions { Headless = true };
        _logger = logger;
    }
    /// <summary>
    /// Generates a PDF from the specified URL.
    /// </summary>
    /// <param name="url">The URL of the webpage to convert to PDF.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the generated PDF as an <see cref="IFormFile"/>.</returns>
    public async Task<IFormFile> GeneratePdfFromUrlAsync(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
			_logger.LogError("GeneratePdfFromUrlAsync: URL cannot be null or empty.");
			throw new ArgumentException("URL cannot be null or empty", nameof(url));
        }

        try
        {
			_logger.LogInformation("Downloading browser for Puppeteer...");
			await _browserFetcher.DownloadAsync();

			var chromeExecutablePath = Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH");
			if (string.IsNullOrEmpty(chromeExecutablePath))
			{
				throw new ApplicationException("Chrome executable path is not set.");
			}

			_logger.LogInformation("Launching browser...");
			var launchOptions = new LaunchOptions
			{
				Headless = true,
				ExecutablePath = chromeExecutablePath,
				Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" } // Recommended for running in Docker
			};

			await using var browser = await Puppeteer.LaunchAsync(launchOptions);

			_logger.LogInformation("Opening new page...");
			await using var page = await browser.NewPageAsync();

			_logger.LogInformation($"Navigating to URL: {url}");
			await page.GoToAsync(url);

			_logger.LogInformation("Generating PDF from page...");
			var pdfData = await page.PdfDataAsync();

            var pdfStream = new MemoryStream(pdfData);
            var pdfFile = new FormFile(pdfStream, 0, pdfData.Length, "file", "scraped_page.pdf");

			_logger.LogInformation("PDF generation successful.");
			return pdfFile;
        }
        catch (Exception ex)
        {
			_logger.LogError(ex, "Failed to generate PDF from URL: {Url}", url);
			throw new ApplicationException("Failed to generate PDF from URL", ex);
        }        
    }    
}