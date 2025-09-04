namespace GameController.Server.VotingServices
{
	public interface IYouTubeChatService
	{
		Task<string?> GetLiveChatIdAsync(string videoId, string accessToken);
		Task<string?> GetLiveChatMessagesAsync(string liveChatId, string? pageToken = null);
		Task PostChatMessageAsync(string liveChatId, string message, string accessToken);
		Task DeleteChatMessageAsync(string messageId, string accessToken); // ახალი მეთოდი



	}
}
