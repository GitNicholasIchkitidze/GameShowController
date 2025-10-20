

using GameController.FBService.Extensions;
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
				_logger.LogErrorWithCaller($"Redis connection failure during lock acquisition for key: {key} {ex}");
				return false;
			}
		}


		public async Task<bool> GetAcquiredLockAsync(string key, TimeSpan expiry)
		{
			try
			{
				
				var db = _connectionMultiplexer.GetDatabase();
				var value = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

				var result = await db.KeyExistsAsync(key);
				return result;
			}
			catch (Exception ex)
			{
				
				_logger.LogErrorWithCaller($"Redis connection failure during lock acquisition for key: {key} {ex}");
				return false;
			}
		}

		public async Task<(bool exists, string? owner)> GetAcquiredLockAsync(string key)
		{
			try
			{
				var db = _connectionMultiplexer.GetDatabase();
				var value = await db.StringGetAsync(key);

				return (value.HasValue, value.HasValue ? value.ToString() : null);
			}
			catch (Exception ex)
			{
				_logger.LogErrorWithCaller($"Redis connection failure while checking lock for key: {key}. {ex}");
				return (false, null);
			}
		}


		// ------------------------------------
		// Rate Limiting / Atomic Increment
		// ------------------------------------
		/// <summary>
		/// Atomically increments the integer value stored at key by one.
		/// If the key does not exist, it is initialized to 0 before the operation.
		/// The expiration is set ONLY if the key was just created (i.e., newCount == 1).
		/// </summary>
		/// <returns>The value after the increment.</returns>
		public async Task<long> IncrementWithExpirationAsync(string key, TimeSpan expiry)
		{
			try
			{
				var db = _connectionMultiplexer.GetDatabase();

				// 1. Atomically increment the value by 1
				var newCount = await db.StringIncrementAsync(key, 1);

				// 2. Set the expiration only if the key was just created (i.e., newCount == 1)
				// This ensures the counter starts fresh every time the expiry window passes.
				if (newCount == 1)
				{
					await db.KeyExpireAsync(key, expiry, CommandFlags.FireAndForget);
				}

				return newCount;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Redis connection failure during IncrementWithExpirationAsync for key: {key}. Returning MaxValue.");
				// კავშირის შეფერხების შემთხვევაში ვაბრუნებთ მაქსიმალურ მნიშვნელობას, რომ შევწყვიტოთ API ზარები
				return long.MaxValue;
			}
		}

		public async Task<long> IncrementAsync(string key, TimeSpan expiry)
		{
			try
			{
				var db = _connectionMultiplexer.GetDatabase();

				// 1. Atomically increment the value by 1
				var newCount = await db.StringIncrementAsync(key, 1);

				// 2. Set the expiration only if the key was just created (i.e., newCount == 1)
				// This ensures the counter automatically resets after the expiry duration.
				if (newCount == 1)
				{
					await db.KeyExpireAsync(key, expiry);
				}

				return newCount;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Redis connection failure during IncrementAsync for key: {key}");
				// Return a maximum value to immediately stop API calls if Redis is down
				return long.MaxValue;
			}
		}


		// ------------------------------------
		// Rate Limiting Increment (NEW)
		// ------------------------------------
		/// <summary>
		/// Atomically increments a key's value by 1 and sets the expiry if it's the first increment.
		/// </summary>
		/// <returns>The new value after increment.</returns>
	
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
