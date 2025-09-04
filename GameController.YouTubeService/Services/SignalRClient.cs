using GameController.Shared;
using GameController.Shared.Models.Connection;
using GameController.Shared.Models.YouTube;
using GameController.YouTubeService.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace YouTubeChatService.Services
{
	public class SignalRClient : ISignalRClient
	{
		private readonly HubConnection _connection;
		private readonly ILogger<SignalRClient> _logger;
		public event Action<VoteRequestMessage>? VotingStateChanged;

		public SignalRClient(HubConnection connection, ILogger<SignalRClient> logger)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));

			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} >>> Youtube Worker SignalR initialized <<<");


			_connection.On<string>("Pong", (message) =>
			{
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} received PONG. from server: {message}{Environment.NewLine}");
			});


			_connection.On<MessageToYTManager>("ReceiveMessageAsync", (message) =>
			{
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Message from server: {message.SenderName}; {message.SenderConnectionId}, {message.MessageText}{Environment.NewLine}");
			});

			_connection.On<VoteRequestMessage>("ReceiveVoteRequest", (message) =>
			{
				VotingStateChanged?.Invoke(message);
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Received Vote Request from Hub: {message.IsVotingActive}");
			});


		}

		/// <summary>
		/// Gets the unique connection ID of the SignalR client.
		/// </summary>
		public string GetConnectionId()
		{
			return _connection.ConnectionId ?? string.Empty;
		}

		/// <summary>
		/// Establishes a connection to the SignalR hub with automatic retries.
		/// </summary>
		public async Task ConnectWithRetryAsync()
		{
			bool connected = false;
			while (!connected)
			{
				try
				{
					await _connection.StartAsync();
					_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Connection to SignalR hub established successfully.");
					await PingServerAsync();
					connected = true;
				}
				catch (HttpRequestException ex)
				{
					_logger.LogError("Failed to connect to SignalR hub: {message}", ex.Message);
					// Wait for the automatic reconnect to handle it.
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to connect to SignalR hub.");
					await Task.Delay(5000); // Wait before retrying
				}
			}
		}






		/// <summary>
		/// Sends a chat message to the SignalR hub.
		/// </summary>
		public async Task SendChatMessageAsync(ChatMessage message)
		{
			try
			{
				await _connection.SendAsync("SendChatMessage", message);
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Sent chat message to hub.");
			}
			catch (Exception ex)
			{
				_logger.LogError($"{Environment.NewLine}{DateTime.Now} Failed to send chat message to hub. {ex}");
			}
		}

		/// <summary>
		/// Sends voting results to the SignalR hub.
		/// </summary>
		public async Task SendVoteResultsAsync(VoteResultsMessage message)
		{
			if (_connection.State == HubConnectionState.Connected)
			{
				await _connection.SendAsync("ReceiveVoteResults", message);
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Sent vote results to GameHub.");
			}
			else
			{
				_logger.LogError($"{Environment.NewLine}{DateTime.Now} Cannot send vote results. Hub connection is not in a 'Connected' state.");
			}
		}

		/// <summary>
		/// Sends the client's connection ID to the SignalR hub.
		/// </summary>
		public async Task SendConnectionIdAsync(ConnectionIdMessage message)
		{
			try
			{
				await _connection.InvokeAsync("SendConnectionId", message);
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Sent connection ID to server: {message.ConnectionId}");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send connection ID to SignalR hub.");
			}
		}



		public async Task ConnectAsync()
		{
			try
			{
				if (_connection.State == HubConnectionState.Disconnected)
				{
					await _connection.StartAsync();
					_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} SignalR client connected to the hub.");
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to connect to the SignalR hub.");
			}
		}

		public async Task DisconnectAsync()
		{
			try
			{
				if (_connection.State != HubConnectionState.Disconnected)
				{
					await _connection.StopAsync();
					_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} SignalR client disconnected from the hub.");
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to disconnect from the SignalR hub.");
			}
		}
		public async Task SendVoteRequestMessageAsync(VoteRequestMessage message)
		{
			try
			{
				await _connection.SendAsync("SendVoteRequestMessage", message);
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Sent vote request message to hub.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send vote request message to hub.");
			}
		}

		public async Task SendVoteSubmissionMessageAsync(VoteSubmissionMessage message)
		{
			try
			{
				await _connection.SendAsync("SendVoteSubmissionMessage", message);
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Sent vote submission message to hub.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send vote submission message to hub.");
			}
		}

		public async Task SendVoteResultsMessageAsync(VoteResultsMessage message)
		{
			try
			{
				await _connection.SendAsync("SendVoteResultsMessage", message);
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Sent vote results message to hub.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send vote results message to hub.");
			}
		}

		public async Task<bool> PingServerAsync()
		{
			try
			{
				await _connection.InvokeAsync("Ping");
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Successfully pinged the server.");
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to ping the server.");
				return false;
			}
		}

		public async Task ReceiveMessageAsync(MessageToYTManager message)
		{
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} received Message From {message.SenderConnectionId} {message.SenderName} {message.MessageText}.");
			

		}
		public async Task Pong()
		{
			
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} received PONG. from server");
		}

	}
}


