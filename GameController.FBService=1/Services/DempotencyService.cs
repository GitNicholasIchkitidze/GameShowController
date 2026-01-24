
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

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

		public async Task<bool> IsDuplicateAsync(string messageId)
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
	}
}
