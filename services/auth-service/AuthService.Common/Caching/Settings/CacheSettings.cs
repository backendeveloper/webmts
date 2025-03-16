namespace AuthService.Common.Caching.Settings;

public class CacheSettings
{
    public string CacheType { get; set; } = "InMemory";

    public DistributedCacheSettings DistributedCache { get; set; } = new DistributedCacheSettings();
}
