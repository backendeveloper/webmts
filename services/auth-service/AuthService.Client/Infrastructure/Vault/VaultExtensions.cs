using VaultSharp;

namespace AuthService.Client.Infrastructure.Vault;

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
            // JWT anahtarını Vault'dan yükle
            var jwtSecrets = await secretManager.GetSecretAsync("webmts/auth/jwt");
            if (jwtSecrets.ContainsKey("key"))
            {
                var memoryConfig = new Dictionary<string, string>
                {
                    {"Jwt:Key", jwtSecrets["key"].ToString()}
                };
                
                builder.AddInMemoryCollection(memoryConfig);
            }
            
            // DB bağlantı bilgilerini Vault'dan yükle
            var dbSecrets = await secretManager.GetSecretAsync("webmts/auth/db");
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
            // Vault'a erişilemediğinde loglama yapılabilir,
            // ancak uygulama çalışmaya devam etmeli
            Console.WriteLine("Could not load secrets from Vault. Using configuration values.");
        }
        
        return builder;
    }
}