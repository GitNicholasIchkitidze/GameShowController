namespace GameController.FBService.Services
{
	public interface IRateLimitingService
	{
		Task<bool> IsRateLimitExceeded();
		Task<bool> IsRateLimitExceeded(string apiEndpoint);
		int LogApiCall();
	}
}
