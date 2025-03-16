using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace AuthService.Common.Caching;

public class DistributedCacheService(IDistributedCache distributedCache) : ICacheService
{
    public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        var jsonData = JsonSerializer.Serialize(value);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        };

        await distributedCache.SetStringAsync(key, jsonData, options);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var jsonData = await distributedCache.GetStringAsync(key);

        return jsonData is null ? default : JsonSerializer.Deserialize<T>(jsonData);
    }

    public Task RemoveAsync(string key)
    {
        return distributedCache.RemoveAsync(key);
    }
}