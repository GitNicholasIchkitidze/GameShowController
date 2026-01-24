using GameController.Shared.Models;
using System.Text.Json.Nodes;

namespace GameController.FBService.Services
{
	public interface IWebhookProcessorService
	{
		Task<OperationResult> ProcessWebhookMessageAsync(string rawPayload);
		
		
		

	}
}
