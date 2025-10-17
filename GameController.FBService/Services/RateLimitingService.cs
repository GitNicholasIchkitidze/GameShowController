using GameController.FBService.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace GameController.FBService.Services
{
	public class RateLimitingService : IRateLimitingService
	{

		
		private readonly ILogger<RateLimitingService> _logger;
		//private const string ApiCallCounterCacheKey = "ApiCallCounter";
		//private const int MaxApiCallsPerHour = 200; // ეს ლიმიტი საწყისია
		private readonly ApplicationDbContext _dbContext; // დაემატა DbContext

		private readonly ICacheService _cacheService; // ICacheService-ის გამოყენება
													  //private const string GlobalApiRateLimitKey = "FB:API:HourlyCount"; // ახალი Redis გასაღები
		private string GlobalApiRateLimitKey;
		private int DefaultMaxApiCallsPerUser;
		private int DefaultHourWindow;




		public RateLimitingService(ILogger<RateLimitingService> logger, ApplicationDbContext dbContext, IConfiguration configuration, ICacheService cacheService)
		{
			
			_logger = logger;
			_dbContext = dbContext;
			_cacheService = cacheService;
			DefaultMaxApiCallsPerUser = configuration.GetValue<int>("DefaultMaxApiCallsPerUser", 200);
			DefaultHourWindow = configuration.GetValue<int>("DefaultHourWindow", 1);
			GlobalApiRateLimitKey = configuration.GetValue<string>("GlobalApiRateLimitKey", "rate:limit:facebook:global:hourly");

		}



		/// <summary>
		/// Checks if the combined rate limit (based on unique users * default limit) is exceeded for a given API endpoint.
		/// ALL API calls are now counted against the single GlobalApiRateLimitKey.
		/// </summary>
		/// <param name="apiEndpoint">A unique identifier for the API call (e.g., "GraphAPI:GetUserName"). Used only for logging/monitoring.</param>
		/// <returns>True if the limit is exceeded, false otherwise.</returns>
		public async Task<bool> IsRateLimitExceeded(string apiEndpoint)
		{
			// 1. Calculate the Dynamic Limit based on Unique Users (This calculation remains unique and complex)
			var uniqueUserCount = await _dbContext.FaceBookVotes
												.Select(v => v.UserId)
												.Distinct()
												.CountAsync();

			// 2. The Total Dynamic Limit for the Hour
			var maxApiCallsPerHour = uniqueUserCount * DefaultMaxApiCallsPerUser;

			// Set a reasonable minimum limit if there are no registered users yet
			if (maxApiCallsPerHour == 0) maxApiCallsPerHour = DefaultMaxApiCallsPerUser * 10;

			// 3. Define Redis Key and Expiration
			// 📣 CRITICAL CHANGE: Use the GLOBAL key for all checks to ensure combined counting
			var cacheKey = GlobalApiRateLimitKey;
			var expiry = TimeSpan.FromHours(DefaultHourWindow);

			// 4. Atomically Increment and Set Expiration
			var currentCount = await _cacheService.IncrementWithExpirationAsync(cacheKey, expiry);

			// 5. Log for transparency (Using the passed apiEndpoint for debugging)
			_logger.LogInformationWithCaller($"[{apiEndpoint}] Global API Count: {currentCount}. Dynamic Limit: {maxApiCallsPerHour}.");


			// 6. Check the limit
			if (currentCount > maxApiCallsPerHour)
			{
				_logger.LogWarningWithCaller($"[RATE LIMIT EXCEEDED] The total dynamic API rate limit for '{apiEndpoint}' has been exceeded. Count: {currentCount}, Limit: {maxApiCallsPerHour}");
				return true;
			}

			return false;
		}
	}
}
