using System.Diagnostics;
using CX.Engine.Common;
using CX.Engine.Common.Embeddings;
using CX.Engine.Importers;
using CX.Engine.Importing.Prod;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CX.Engine.Importing;

public static class StartupImporters
{
    public static async Task StartupVectormindProdImporterAsync(IHost host)
    {
        var sw = Stopwatch.StartNew();
        var swTotal = Stopwatch.StartNew();
        var prodImporter = host.Services.GetRequiredService<VectormindProdImporter>();
        var embeddingCache = host.Services.GetRequiredService<EmbeddingCache>();
        var logger = host.Services.GetLogger(typeof(StartupImporters).FullName);
        var documents = 0;
        prodImporter.DocumentImported += (_, _) =>
        {
            Interlocked.Increment(ref documents);
            lock (sw)
                if (sw.Elapsed > TimeSpan.FromMinutes(1))
                {
                    sw.Restart();
                    logger.LogInformation($"""
                                           Documents Imported: {documents:#,##0} ({documents / swTotal.Elapsed.TotalHours:#,##0} per hour)
                                           Embeddings in Cache: {embeddingCache.CacheEntries}
                                           """);
                    embeddingCache.Save();
                }
        };
        await prodImporter.ImportAsync();
        logger.LogInformation($"""
                               Documents Imported: {documents:#,##0} ({documents / swTotal.Elapsed.TotalHours:#,##0} per hour)
                               Embeddings in Cache: {embeddingCache.CacheEntries}
                               """);
        embeddingCache.Save();
    }

    public static async Task StartupDiskImporterAsync(IHost host)
    {
        var sw = Stopwatch.StartNew();
        var swTotal = Stopwatch.StartNew();
        var diskImporter = host.Services.GetRequiredService<DiskImporter>();
        var embeddingCache = host.Services.GetRequiredService<EmbeddingCache>();
        var logger = host.Services.GetLogger(typeof(StartupImporters).FullName);
        var documents = 0;
        diskImporter.DocumentImported += (_, _) =>
        {
            Interlocked.Increment(ref documents);
            lock (sw)
                if (sw.Elapsed > TimeSpan.FromMinutes(1))
                {
                    sw.Restart();
                    logger.LogInformation($"""
                                           Documents Imported: {documents:#,##0} ({documents / swTotal.Elapsed.TotalHours:#,##0} per hour)
                                           Embeddings in Cache: {embeddingCache.CacheEntries}
                                           """);
                    embeddingCache.Save();
                }
        };
        await diskImporter.ImportFromOptionsAsync();
        logger.LogInformation($"""
                               Documents Imported: {documents:#,##0} ({documents / swTotal.Elapsed.TotalHours:#,##0} per hour)
                               Embeddings in Cache: {embeddingCache.CacheEntries}
                               """);
        embeddingCache.Save();
    }
}