namespace GameController.FBService.Services
{
	public interface IRateLimitingService
	{
		Task<bool> IsRateLimitExceeded();
		int LogApiCall();
	}
}
