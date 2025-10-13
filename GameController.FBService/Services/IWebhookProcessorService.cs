namespace GameController.FBService.Services
{
	public interface IWebhookProcessorService
	{
		Task ProcessWebhookMessageAsync(string rawPayload);

	}
}
