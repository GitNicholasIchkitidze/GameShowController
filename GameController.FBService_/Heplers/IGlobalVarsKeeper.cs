namespace GameController.FBService.Heplers
{
	public interface IGlobalVarsKeeper
	{
		Task SetValueAsync<T>(string key, T value);
		Task<T?> GetValueAsync<T>(string key);
		Task<bool> ExistsAsync(string key);
		Task RemoveAsync(string key);
		Task<IEnumerable<string>> GetAllKeysAsync(string? pattern = null);
		Task<Dictionary<string, string>> GetAllKeysAndValuesAsync(string? pattern = null);
	}
}
