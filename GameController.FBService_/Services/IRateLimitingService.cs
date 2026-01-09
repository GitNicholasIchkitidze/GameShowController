namespace GameController.FBService.Services
{
	public interface IRateLimitingService
	{
		
		Task<bool> IsRateLimitExceeded(string apiEndpoint);
		//int LogApiCall();
	}
}
