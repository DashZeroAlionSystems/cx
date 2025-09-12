using System.Text;
using CX.Engine.Common.PostgreSQL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CX.Engine.Common.Storage.PostgreSQLFileStorage;

public class PostgreSQLStorageService : IStorageService, ISnapshottedOptions<PostgreSQLStorageService.Snapshot, PostgreSQLStorageServiceOptions, PostgreSQLStorageService>
{
    public ILogger _logger { get; set; }
    public IServiceProvider _sp { get; set; }
    public string _name { get; set; }
    
    public class Snapshot : Snapshot<PostgreSQLStorageServiceOptions, PostgreSQLStorageService>, ISnapshotSyncInit<PostgreSQLStorageServiceOptions>
    {
        public PostgreSQLClient Client;
        public void Init(IConfigurationSection section, ILogger logger, IServiceProvider sp)
        {
            sp.GetRequiredNamedService(out Client, Options.PostgeSqlClientName, section);
        }
    }
    
    public PostgreSQLStorageService(MonitoredOptionsSection<PostgreSQLStorageServiceOptions> opts, ILogger logger, IServiceProvider sp, string name)
    {
        _logger = logger;
        _sp = sp;
        opts.Bind<Snapshot, PostgreSQLStorageService>(this);
        _name = name;
    }

    public Snapshot CurrentShapshot { get; set; }
    public MonitoredOptionsSection<PostgreSQLStorageServiceOptions> OptionsSection { get; set; }
    private StorageResponseBase GetStream(string content) => new ()
    {
        Content = new MemoryStream(Encoding.UTF8.GetBytes(content)), 
        ContentType = MimeType.Txt
    };
    
    public async Task<StorageResponseBase> GetContentAsync(string id)
    {
        if(id == string.Empty)
            throw new ArgumentNullException($"{nameof(GetContentAsync)}.{nameof(id)} cannot be null");
        
        
        var ss = CurrentShapshot;
        var opts = ss.Options;
        var client = ss.Client;

        /*name = name.Replace(" ", "%20");*/
        
        var contentObj = await client.ListStringAsync($"SELECT content FROM {new InjectRaw(opts.RelationName)} WHERE id = {new Guid(id)}");
        if(contentObj == null)
            throw new ArgumentNullException($"Document with id: {id} and with id: {id} does not exist");
        
        var content = contentObj.FirstOrDefault();
        
        if(string.IsNullOrWhiteSpace(content))
            throw new ArgumentNullException($"Document with id: {id} and with id: {id} does not exist");
        
        /*content = content
            .Replace("\\n", "\r\n")
            .Replace("\\n", "\n")
            .Replace("''", "'");*/
        
        return GetStream(content);
    }

    public async Task<string> GetContentUrlAsync(string id)
    {
        var ss = CurrentShapshot;
        var opts = ss.Options;
        var client = ss.Client;
        var guid = new Guid(id);
        var name = await client.ListStringAsync($"SELECT name FROM {new InjectRaw(opts.RelationName)} WHERE id = {guid}");
        if(name is null || !name.Any())
            throw new ArgumentNullException($"Document with id: {id} and with id: {id} does not exist");
        
        return Url.Combine(opts.BaseUrl,
            $"?id={id}&name={name.FirstOrDefault()}&store_provider={_name}");
    }

    public async Task<string> InsertContentAsync(string name, Stream content)
    {
        var ss = CurrentShapshot;
        var opts = ss.Options;
        var client = ss.Client;
        var id = Guid.NewGuid();
        
        if(string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException($"{nameof(InsertContentAsync)}.{nameof(name)} cannot be null");
        
        if(content is null)
            throw new ArgumentNullException($"{nameof(InsertContentAsync)}.{nameof(content)} cannot be null");
        
        var reader = new StreamReader(content);
        await client.ExecuteAsync($"INSERT INTO {new InjectRaw(opts.RelationName)} (id, name, content) VALUES ({id}, {name}, {await reader.ReadToEndAsync()})");

        return id.ToString();

    }
    
    public async Task DeleteContentAsync(string id)
    {
        var ss = CurrentShapshot;
        var opts = ss.Options;
        var client = ss.Client;
        
        if(id == string.Empty)
            throw new ArgumentNullException($"{nameof(DeleteContentAsync)}.{nameof(id)} cannot be null");

        await client.ExecuteAsync($"DELETE FROM {new InjectRaw(opts.RelationName)} WHERE id = '{id}'");
    }

    public async Task<List<StorageResponseBase>> GetContentsAsync()
    {
        var ss = CurrentShapshot;
        var opts = ss.Options;
        var client = ss.Client;

        var res = new List<StorageResponseBase>();
        var contents = await client.ListStringAsync($"SELECT content FROM {new InjectRaw(opts.RelationName)}");
        foreach (var content in contents)
            res.Add(GetStream(content));
        
        return res;
    }
}