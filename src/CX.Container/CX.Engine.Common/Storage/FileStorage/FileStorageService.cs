using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CX.Engine.Common.Storage.FileStorage;

public class FileStorageService : IStorageService, ISnapshottedOptions<FileStorageService.Snapshot, FileStorageServiceOptions, FileStorageService>
{
    public ILogger _logger { get; set; }
    public IServiceProvider _sp { get; set; }
    public string _name { get; set; }
    
    public class Snapshot : Snapshot<FileStorageServiceOptions, FileStorageService>, ISnapshotSyncInit<FileStorageServiceOptions>
    {
        public void Init(IConfigurationSection section, ILogger logger, IServiceProvider sp)
        {
        }
    }
    
    public FileStorageService(MonitoredOptionsSection<FileStorageServiceOptions> opts, ILogger logger, IServiceProvider sp, string name)
    {
        _logger = logger;
        _sp = sp;
        opts.Bind<Snapshot, FileStorageService>(this);
        _name = name;
    }

    public StorageResponseBase GetResponse(Stream stream)
    {
        return new()
        {
            Content = stream,
            ContentType = MimeType.Txt
        };
    }
    
    /// <summary>
    ///   Reads and discards the first line (up to the first newline),
    ///   then returns a new MemoryStream containing all remaining text.
    /// </summary>
    private async Task<(Stream stream, string line)> ReadRemoveFirstLineAsync(Stream input)
    {
        // Wrap in a reader but do not close the underlying stream.
        using var reader = new StreamReader(input, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        // 1) Skip/discard the first line
        var line = await reader.ReadLineAsync();

        // 2) Read the rest of the text
        var remainder = await reader.ReadToEndAsync();

        // 3) Write it into a new MemoryStream and rewind
        var ms = new MemoryStream(Encoding.UTF8.GetBytes(remainder));
        ms.Position = 0;
        return (ms, line);
    }
    
    public async Task<StorageResponseBase> GetContentAsync(string id)
    {
        var ss = CurrentShapshot;
        var opts = ss.Options;
        
        if(id == string.Empty)
            throw new ArgumentNullException($"{nameof(GetContentAsync)}.{nameof(id)} cannot be null");
        var path = Path.Combine(opts.BasePath, $"{id}.txt");
        var content = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return GetResponse((await ReadRemoveFirstLineAsync(content)).stream);
    }

    public async Task<string> GetContentUrlAsync(string id)
    {
        var ss = CurrentShapshot;
        var opts = ss.Options;

        var res = await ReadRemoveFirstLineAsync(new FileStream(Path.Combine(opts.BasePath, $"{id}.txt"), FileMode.Open, FileAccess.Read, FileShare.Read));
        
        var name = res.line;
        if(string.IsNullOrEmpty(name))
            throw new ArgumentNullException($"Document with id: {id} and with id: {id} does not exist");
        
        return Url.Combine(opts.BaseUrl,
            $"?id={id}&name={name.FirstOrDefault()}&store_provider={_name}");
    }

    public async Task<string> InsertContentAsync(string name, Stream content)
    {
        var opts = CurrentShapshot.Options;
        var id = Guid.NewGuid();
        
        if(string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException($"{nameof(InsertContentAsync)}.{nameof(name)} cannot be null");

        if (content is null)
            throw new ArgumentNullException($"{nameof(InsertContentAsync)}.{nameof(content)} cannot be null");

        await content.WriteAsync(Encoding.UTF8.GetBytes(name + "\n"));
        
        var path = Path.Combine(opts.BasePath, $"{id}.txt");
        
        var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        await content.CopyToAsync(file);
        file.Close();
        return id.ToString();
    }

    public Task DeleteContentAsync(string id)
    {
        var ss = CurrentShapshot;
        var opts = CurrentShapshot.Options;
        
        if(id == string.Empty)
            throw new ArgumentNullException($"{nameof(DeleteContentAsync)}.{nameof(id)} cannot be null");
        
        var path = Path.Combine(opts.BasePath, $"{id}.txt");
        File.Delete(path);
        return Task.CompletedTask;
    }

    public Task<List<StorageResponseBase>> GetContentsAsync()
    {
        var ss = CurrentShapshot;
        var opts = CurrentShapshot.Options;
        
        //Read directory files and return a list of strings
        var items = new List<StorageResponseBase>();
        var files = Directory.GetFiles(opts.BasePath, "*.*", SearchOption.AllDirectories);
        foreach (var file in files)
            items.Add(GetResponse(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read)));
        return Task.FromResult(items);
    }

    public Snapshot CurrentShapshot { get; set; }
    public MonitoredOptionsSection<FileStorageServiceOptions> OptionsSection { get; set; }
}