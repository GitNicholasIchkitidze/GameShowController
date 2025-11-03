using GameController.Shared.Models;
using System.Text.Json.Nodes;

namespace GameController.FBService.Services
{
	public interface IWebhookProcessorService
	{
		Task<OperationResult> ProcessWebhookMessageAsync(string rawPayload);
		string? ExtractMessagePostbackPayLoad(JsonObject payload);
		string? ExtractMessageText(JsonObject payload);
		string? ExtractMessageSenderOrRecipientId(JsonObject payload, string type = "sender");
		string? ExtractMessageId(JsonObject payload, string messageType);
		string? ExtractMessageType(JsonObject payload);
	}
}
