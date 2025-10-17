namespace GameController.FBService.Services
{
	public interface ICacheService
	{
		// For atomic vote check/lock
		Task<bool> AcquireLockAsync(string key, TimeSpan expiry);
		Task ReleaseLockAsync(string key);

		// For general data caching (e.g., User Name)
		Task<T> GetAsync<T>(string key);
		Task SetAsync<T>(string key, T value, TimeSpan absoluteExpiration);

		Task<long> IncrementAsync(string key, TimeSpan expiry);
		Task<long> IncrementWithExpirationAsync(string key, TimeSpan expiry);

	}
}
