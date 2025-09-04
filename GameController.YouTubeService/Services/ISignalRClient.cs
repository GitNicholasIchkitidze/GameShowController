using GameController.Shared.Models.Connection;
using GameController.Shared.Models.YouTube;

namespace GameController.YouTubeService.Services
{
	public interface ISignalRClient
	{
		/// <summary>
		/// An event that is triggered when the voting state changes on the server.
		/// </summary>
		event Action<VoteRequestMessage>? VotingStateChanged;

		/// <summary>
		/// Gets the unique connection ID of the SignalR client.
		/// </summary>
		string GetConnectionId();

		/// <summary>
		/// Establishes a connection to the SignalR hub with automatic retries.
		/// </summary>
		Task ConnectWithRetryAsync();

		/// <summary>
		/// Sends a chat message to the SignalR hub.
		/// </summary>
		Task SendChatMessageAsync(ChatMessage message);

		/// <summary>
		/// Sends voting results to the SignalR hub.
		/// </summary>
		Task SendVoteResultsAsync(VoteResultsMessage message);

		/// <summary>
		/// Sends a ping to the server to check the connection.
		/// </summary>
		Task<bool> PingServerAsync();
		Task Pong();

		/// <summary>
		/// Sends the client's connection ID to the SignalR hub.
		/// </summary>
		Task SendConnectionIdAsync(ConnectionIdMessage message);

		Task ConnectAsync();		
		Task DisconnectAsync();		
		Task SendVoteRequestMessageAsync(VoteRequestMessage message);
		Task SendVoteSubmissionMessageAsync(VoteSubmissionMessage message);
		Task SendVoteResultsMessageAsync(VoteResultsMessage message);

		Task ReceiveMessageAsync(MessageToYTManager message);

	}
}
