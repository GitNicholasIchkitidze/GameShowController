using System.Text.Json.Nodes;

namespace GameController.FBService.Services
{
	public interface IWebhookProcessorService
	{
		Task ProcessWebhookMessageAsync(string rawPayload);
		string? ExtractMessagePostbackPayLoad(JsonObject payload);
		string? ExtractMessageText(JsonObject payload);
		string? ExtractMessageSenderId(JsonObject payload);
		string? ExtractMessageId(JsonObject payload, string messageType);
		string? ExtractMessageType(JsonObject payload);
	}
}
