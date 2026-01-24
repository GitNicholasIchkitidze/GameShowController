
using StackExchange.Redis;
using System;
using System.Threading.Tasks;
using GameController.FBService.Heplers; // ახალი დამატებული


namespace GameController.FBService.Services
{
	public class DempotencyService : IDempotencyService
	{
		private readonly IDatabase _redisDb;
		private const int DEFAULT_TTL_HOURS = 24;

		public DempotencyService(IConnectionMultiplexer redis)
		{
			_redisDb = redis.GetDatabase();
		}

		public async Task<bool> IsDuplicateAsync_(string messageId)
		{
			if (string.IsNullOrEmpty(messageId))
				return false;

			// Redis key pattern: fb:msg:<id>
			string key = $"fb:msg:{messageId}";

			// Try to set if not exists
			bool isNew = await _redisDb.StringSetAsync(
				key,
				"1",
				TimeSpan.FromHours(DEFAULT_TTL_HOURS),
				When.NotExists
			);

			// If value was already set, message is duplicate
			return !isNew;
		}

		public async Task<bool> IsDuplicateAsync(string messageId)
		{
			string legacyKey = $"fb:msg:{messageId}"; // ახალი დამატებული

			// NEW standardized key (namespaced)
			string key = RedisKeys.FB.Native.IdempotencyMessage(messageId); // ახალი დამატებული

			var ttl = TimeSpan.FromHours(DEFAULT_TTL_HOURS); // ახალი დამატებული

			// Try to set BOTH keys if not exists (supports mixed versions running together)
			bool isNew1 = await _redisDb.StringSetAsync(key, "1", ttl, When.NotExists);        // ახალი დამატებული
			bool isNew2 = await _redisDb.StringSetAsync(legacyKey, "1", ttl, When.NotExists); // ახალი დამატებული

			// If either already existed -> duplicate
			return !(isNew1 && isNew2); // ახალი დამატებული
		}
	}
}
