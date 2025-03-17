// using System.Text;
// using Consul;
//
// namespace AuthService.Client.Infrastructure.Consul;
//
// public class ConsulConfigurationProvider(
//     IConsulClient consulClient,
//     string serviceName,
//     ILogger<ConsulConfigurationProvider> logger)
//     : ConfigurationProvider
// {
//     public override void Load() => LoadAsync().Wait();
//
//     private async Task LoadAsync()
//     {
//         try
//         {
//             var pairs = await consulClient.KV.List($"{serviceName}/config");
//             if (pairs.Response == null)
//             {
//                 logger.LogWarning("No configuration found in Consul for {ServiceName}", serviceName);
//                 return;
//             }
//
//             foreach (var pair in pairs.Response)
//             {
//                 if (pair.Value == null) continue;
//
//                 var key = pair.Key
//                     .Replace($"{serviceName}/config/", string.Empty)
//                     .Replace("/", ":");
//
//                 var value = Encoding.UTF8.GetString(pair.Value);
//
//                 Data[key] = value;
//                 logger.LogDebug("Loaded configuration from Consul: {Key} = {Value}", key, value);
//             }
//         }
//         catch (Exception ex)
//         {
//             logger.LogError(ex, "Error loading configuration from Consul for {ServiceName}", serviceName);
//         }
//     }
// }