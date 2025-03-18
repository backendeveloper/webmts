namespace TransactionService.Common.ServiceDiscovery.Interfaces;

public interface IKeyValueStore
{
    Task<bool> SetValueAsync(string key, string value);
    Task<bool> SetValueAsync<T>(string key, T value) where T : class;
    Task<string> GetValueAsync(string key);
    Task<T> GetValueAsync<T>(string key) where T : class;
    Task<bool> DeleteValueAsync(string key);
    Task<IDictionary<string, string>> GetAllValuesAsync(string prefix);
    Task<bool> SyncConfigurationSectionToConsulAsync(string sectionName, string consulPrefix);
    Task<bool> SyncAllConfigurationsToConsulAsync(string serviceName);
}