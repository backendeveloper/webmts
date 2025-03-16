using Microsoft.Extensions.Caching.Memory;

namespace AuthService.Common.Caching;

public class InMemoryCacheService(IMemoryCache memoryCache) : ICacheService
{
    public Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        memoryCache.Set(key, value, expiration);
        
        return Task.CompletedTask;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        return Task.FromResult(memoryCache.TryGetValue(key, out T? value) ? value : default);
    }

    public Task RemoveAsync(string key)
    {
        memoryCache.Remove(key);
        
        return Task.CompletedTask;
    }
}