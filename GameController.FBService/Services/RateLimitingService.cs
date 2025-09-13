using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace GameController.FBService.Services
{
	public class RateLimitingService : IRateLimitingService
	{

		private readonly IMemoryCache _cache;
		private readonly ILogger<RateLimitingService> _logger;
		private const string ApiCallCounterCacheKey = "ApiCallCounter";
		private const int MaxApiCallsPerHour = 200; // ეს ლიმიტი საწყისია
		private readonly ApplicationDbContext _dbContext; // დაემატა DbContext

		public RateLimitingService(IMemoryCache cache, ILogger<RateLimitingService> logger, ApplicationDbContext dbContext)
		{
			_cache = cache;
			_logger = logger;
			_dbContext = dbContext;
		}

		public int LogApiCall()
		{
			var cacheEntryOptions = new MemoryCacheEntryOptions()
					   .SetAbsoluteExpiration(TimeSpan.FromHours(1)); // ქეში გაიწმინდება 1 საათში

			var currentCount = _cache.GetOrCreate(ApiCallCounterCacheKey, entry =>
			{
				entry.SetOptions(cacheEntryOptions);
				return 0;
			});

			_cache.Set(ApiCallCounterCacheKey, currentCount + 1, cacheEntryOptions);
			_logger.LogInformation($"API Call logged. Current count: {currentCount + 1}");

			return currentCount + 1;
		}

		public async Task<bool> IsRateLimitExceeded()
		{

			var uniqueUserCount = await _dbContext.FaceBookVotes
												  .Select(v => v.UserId)
												  .Distinct()
												  .CountAsync();

			var maxApiCallsPerHour = uniqueUserCount * 200;
			if (_cache.TryGetValue(ApiCallCounterCacheKey, out int currentCount))
			{
				if (currentCount >= MaxApiCallsPerHour)
				{
					_logger.LogWarning($"API rate limit exceeded. Current count: {currentCount}. Limit: {MaxApiCallsPerHour}");
					return true;
				}
			}
			return false;
		}
	}
}
