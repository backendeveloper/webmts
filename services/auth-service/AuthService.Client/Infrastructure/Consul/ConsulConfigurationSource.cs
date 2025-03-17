// using Consul;
//
// namespace AuthService.Client.Infrastructure.Consul;
//
// public class ConsulConfigurationSource(
//     IConsulClient consulClient,
//     string serviceName,
//     ILogger<ConsulConfigurationProvider> logger)
//     : IConfigurationSource
// {
//     public IConfigurationProvider Build(IConfigurationBuilder builder) =>
//         new ConsulConfigurationProvider(consulClient, serviceName, logger);
// }