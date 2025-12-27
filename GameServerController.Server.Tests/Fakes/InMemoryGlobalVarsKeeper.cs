using GameController.FBService.Heplers;
using System.Text.Json;

namespace GameController.FBService.Tests.Fakes;

public class InMemoryGlobalVarsKeeper : IGlobalVarsKeeper
{
    private readonly Dictionary<string, string> _store = new();



    private readonly JsonSerializerOptions _opt = new(JsonSerializerDefaults.Web);

    public event Func<string, string?, Task>? OnChanged;

    public Task<T?> GetValueAsync<T>(string key)
    {
        if (!_store.TryGetValue(key, out var json))
            return Task.FromResult<T?>(default);

        return Task.FromResult(JsonSerializer.Deserialize<T>(json, _opt));
    }



    public async Task SetValueAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value, _opt);
        _store[key] = json;

        if (OnChanged != null)
            await OnChanged.Invoke(key, json);
    }



    public Task<bool> DeleteKeyAsync(string key)
        => Task.FromResult(_store.Remove(key));

    public Task<bool> ExistsAsync(string key)
        => Task.FromResult(_store.ContainsKey(key));

    public Task RemoveAsync(string key)
    {
        _store.Remove(key);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetAllKeysAsync(string? pattern = null)
        => Task.FromResult<IEnumerable<string>>(_store.Keys.ToList());

    public Task<Dictionary<string, string>> GetAllKeysAndValuesAsync(string? pattern = null)
        => Task.FromResult(new Dictionary<string, string>(_store));
}
