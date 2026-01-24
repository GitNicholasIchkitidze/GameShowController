using GameController.FBService.Extensions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace GameController.FBService.Services
{
	public class SignalRClient : ISignalRClient
	{

		private readonly HubConnection _connection;
		private readonly ILogger<SignalRClient> _logger;
		private readonly IConfiguration _configuration;
		public SignalRClient(IConfiguration configuration, ILogger<SignalRClient> logger, HubConnection connection)
		{
			_logger = logger;
			_configuration = configuration;
			_connection = connection;
		}


		public async Task ConnectWithRetryAsync()
		{
			bool connected = false;
			while (!connected)
			{
				try
				{
					await _connection.StartAsync();
					_logger.LogInformationWithCaller($"{Environment.NewLine}{DateTime.Now} Connection to SignalR hub established successfully.");
					//await PingServerAsync();
					connected = true;
				}
				catch (HttpRequestException ex)
				{
					_logger.LogErrorWithCaller($"Failed to connect to SignalR hub: {ex}");
					// Wait for the automatic reconnect to handle it.
				}
				catch (Exception ex)
				{
					_logger.LogErrorWithCaller( $"Failed to connect to SignalR hub. {ex}");
					await Task.Delay(5000); // Wait before retrying
				}
			}
		}



		public async Task SendVoteToHub(string candidateName)
		{
			try
			{
				if (_connection.State != HubConnectionState.Connected)
				{
					await ConnectWithRetryAsync();
				}

				// აქ ვგზავნით მონაცემებს "ReceiveVote" მეთოდით SignalR-ის ჰაბზე.
				await _connection.InvokeAsync("ReceiveVote", candidateName);
				_logger.LogInformationWithCaller($"Sent vote for candidate: {candidateName} to hub.");
			}
			catch (System.Exception ex)
			{
				_logger.LogErrorWithCaller($"Failed to send vote to SignalR hub: {ex.Message}");
			}
		}
	}
}
