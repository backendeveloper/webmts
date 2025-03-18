using VaultSharp;

namespace CustomerService.Client.Infrastructure.Vault;

public class VaultSecretManager : ISecretManager
{
    private readonly IVaultClient _vaultClient;
    private readonly ILogger<VaultSecretManager> _logger;

    public VaultSecretManager(IVaultClient vaultClient, ILogger<VaultSecretManager> logger)
    {
        _vaultClient = vaultClient;
        _logger = logger;
    }

    public async Task<Dictionary<string, object>?> GetSecretAsync(string path)
    {
        try
        {
            var secret = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path, mountPoint: "secret");
            
            return secret.Data.Data as Dictionary<string, object>;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving secret from path {Path}", path);
            return new Dictionary<string, object>();
        }
    }

    public async Task SetSecretAsync(string path, Dictionary<string, object?> secrets)
    {
        try
        {
            await _vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(path, secrets, mountPoint: "secret");
            _logger.LogInformation("Secret stored at path {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing secret at path {Path}", path);
            throw;
        }
    }
}