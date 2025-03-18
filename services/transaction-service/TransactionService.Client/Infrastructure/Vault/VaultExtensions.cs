using VaultSharp;

namespace TransactionService.Client.Infrastructure.Vault;

public static class VaultExtensions
{
    public static IServiceCollection AddVaultServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IVaultClient>(provider => {
            var vaultUrl = configuration["Vault:Url"] ?? "http://vault:8200";
            var vaultToken = configuration["Vault:Token"] ?? "webmts-root-token";
            
            var tokenAuthMethod = new VaultSharp.V1.AuthMethods.Token.TokenAuthMethodInfo(vaultToken);
            var vaultClientSettings = new VaultClientSettings(vaultUrl, tokenAuthMethod);
            
            return new VaultClient(vaultClientSettings);
        });

        services.AddSingleton<ISecretManager, VaultSecretManager>();

        return services;
    }
    
    public static async Task<IConfigurationBuilder> AddVaultConfiguration(this IConfigurationBuilder builder, IConfiguration configuration)
    {
        var vaultUrl = configuration["Vault:Url"] ?? "http://vault:8200";
        var vaultToken = configuration["Vault:Token"] ?? "webmts-root-token";
        
        var tokenAuthMethod = new VaultSharp.V1.AuthMethods.Token.TokenAuthMethodInfo(vaultToken);
        var vaultClientSettings = new VaultClientSettings(vaultUrl, tokenAuthMethod);
        var vaultClient = new VaultClient(vaultClientSettings);
        
        var secretManager = new VaultSecretManager(
            vaultClient, 
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<VaultSecretManager>());
        
        try
        {
            var jwtSecrets = await secretManager.GetSecretAsync("webmts/transaction/jwt");
            if (jwtSecrets.ContainsKey("key"))
            {
                var memoryConfig = new Dictionary<string, string>
                {
                    {"Jwt:Key", jwtSecrets["key"].ToString()}
                };
                
                builder.AddInMemoryCollection(memoryConfig);
            }
            
            var dbSecrets = await secretManager.GetSecretAsync("webmts/transaction/db");
            if (dbSecrets.ContainsKey("connectionString"))
            {
                var memoryConfig = new Dictionary<string, string>
                {
                    {"ConnectionStrings:DefaultConnection", dbSecrets["connectionString"].ToString()}
                };
                
                builder.AddInMemoryCollection(memoryConfig);
            }
        }
        catch (Exception)
        {
            Console.WriteLine("Could not load secrets from Vault. Using configuration values.");
        }
        
        return builder;
    }
}