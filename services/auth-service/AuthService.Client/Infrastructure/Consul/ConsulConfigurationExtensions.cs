// using Consul;
//
// namespace AuthService.Client.Infrastructure.Consul;
//
// public static class ConsulConfigurationExtensions
// {
//     public static IConfigurationBuilder AddConsul(
//         this IConfigurationBuilder builder,
//         IConsulClient consulClient,
//         string serviceName,
//         ILogger<ConsulConfigurationProvider> logger) =>
//         builder.Add(new ConsulConfigurationSource(consulClient, serviceName, logger));
// }