using AuthService.Common.Caching.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Common.Caching;

public static class DependencyInjection
{
    public static void AddCachingServices(this IServiceCollection services, IConfiguration configuration)
    {
        var cacheSettings = configuration.GetSection("CacheSettings").Get<CacheSettings>();

        if (cacheSettings?.CacheType == "DistributedCache")
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = cacheSettings.DistributedCache.ConnectionString;
                options.InstanceName = cacheSettings.DistributedCache.InstanceName;
            });
            services.AddScoped<ICacheService, DistributedCacheService>();
        }
        else
        {
            services.AddMemoryCache();
            services.AddScoped<ICacheService, InMemoryCacheService>();
        }
    }
}