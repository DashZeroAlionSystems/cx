namespace CX.Engine.Common.PostgreSQL;

public class QueryCacheOptions
{
    public TimeSpan CacheEntryExpiresAfter { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan StartCacheEntryRefreshAfter { get; set; } = TimeSpan.FromSeconds(20);
}