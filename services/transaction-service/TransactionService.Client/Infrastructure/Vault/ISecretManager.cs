namespace TransactionService.Client.Infrastructure.Vault;

public interface ISecretManager
{
    Task<Dictionary<string, object>?> GetSecretAsync(string path);
    Task SetSecretAsync(string path, Dictionary<string, object?> secrets);
}