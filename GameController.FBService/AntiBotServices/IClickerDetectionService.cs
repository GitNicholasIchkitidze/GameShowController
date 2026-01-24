using GameController.FBService.Models;

namespace GameController.FBService.AntiBotServices
	
{


	public interface IClickerDetectionService
	{
		Task<ClickerDecision> EvaluateAsync(string recipientId, string userId, string userName, DateTime utcNow);
		Task<Boolean> ApplyClikerMetrics(string clientName, ClickerDecision decision);

    }
}
