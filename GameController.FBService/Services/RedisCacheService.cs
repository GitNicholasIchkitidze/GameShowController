

using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;

namespace GameController.FBService.Services
{
	public class RedisCacheService : ICacheService
	{
		private readonly IDistributedCache _cache;
		private readonly IConnectionMultiplexer _connectionMultiplexer; // 2. NEW: Dependency
		private readonly ILogger<RedisCacheService> _logger;


		public RedisCacheService(IDistributedCache cache, IConnectionMultiplexer connectionMultiplexer, ILogger<RedisCacheService> logger)
		{
			_cache = cache;
			_connectionMultiplexer = connectionMultiplexer;
			_logger = logger;
		}
				

		// ------------------------------------
		// Locking (CRITICALLY FIXED FOR ATOMICITY)
		// ------------------------------------
		public async Task<bool> AcquireLockAsync(string key, TimeSpan expiry)
		{
			try
			{
				// Get the native Redis database instance
				var db = _connectionMultiplexer.GetDatabase();

				// The value is an arbitrary unique ID (or timestamp)
				var value = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

				// THIS IS THE ATOMIC OPERATION: SET NX EX
				// It returns TRUE ONLY if the key did not already exist.
				return await db.StringSetAsync(
					key,
					value,
					expiry,
					When.NotExists
				);
			}
			catch (Exception ex)
			{
				// Log the connection error, but assume lock acquisition failed
				_logger.LogError(ex, $"Redis connection failure during lock acquisition for key: {key}");
				return false;
			}
		}


		public Task ReleaseLockAsync(string key)
		{
			// The current implementation is fine, as we rely on expiration, 
			// but we can still remove the key if needed.
			return _cache.RemoveAsync(key);
		}

		// ------------------------------------
		// Caching (Crucial for Facebook API optimization)
		// ------------------------------------
		public async Task<T> GetAsync<T>(string key)
		{
			var value = await _cache.GetStringAsync(key);
			if (value == null) return default;
			return JsonSerializer.Deserialize<T>(value);
		}

		public Task SetAsync<T>(string key, T value, TimeSpan absoluteExpiration)
		{
			var json = JsonSerializer.Serialize(value);
			return _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = absoluteExpiration
			});
		}
	}
}
