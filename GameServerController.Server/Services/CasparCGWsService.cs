// CasparCGWsService.cs
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;

namespace GameController.Server.Services
{
	public class CasparCGWsService : ICasparCGWsService
	{
		private readonly ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();
		private readonly ConcurrentDictionary<string, string> _connectionIdToTemplateType = new ConcurrentDictionary<string, string>();
		private readonly ConcurrentDictionary<string, string> _templateTypeToConnectionId = new ConcurrentDictionary<string, string>();

		private readonly ILogger<CasparCGWsService> _logger;
		public CasparCGWsService(ILogger<CasparCGWsService> logger)
		{
			_logger = logger;
			
		}
		public void AddConnection(WebSocket socket)
		{
			var connectionId = Guid.NewGuid().ToString();
			_sockets.TryAdd(connectionId, socket);

			_logger.LogInformation($"{DateTime.Now} WebSocket connection established with ID: {connectionId}");

			// Start listening for a registration message from the client
			_ = ReceiveRegistrationAsync(socket, connectionId);
		}

		public async Task ReceiveRegistrationAsync(WebSocket socket, string connectionId)
		{
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now}  Entered in ReceiveRegistrationAsync:' connectionId: {connectionId}' socketState: {socket.State.ToString()}");

			var buffer = new byte[1024 * 4];
			var receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

			if (receiveResult.MessageType == WebSocketMessageType.Text)
			{
				var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
				try
				{
					var data = JsonConvert.DeserializeObject<dynamic>(message);
					if (data.type == "register" && data.templateName != null)
					{
						string templateName = data.templateName.ToString();
						_connectionIdToTemplateType[connectionId] = templateName;
						_templateTypeToConnectionId[templateName] = connectionId;
						_logger.LogInformation($"{Environment.NewLine}{DateTime.Now}  Template '{templateName}' successfully registered.{Environment.NewLine}");
					}
				}
				catch (Exception ex)
				{
					_logger.LogInformation($"{Environment.NewLine}{DateTime.Now}  Failed to parse registration message from client {connectionId}: {ex.Message}");
				}
			}
		}

		/// <summary>
		/// Sends data to a specific template type.
		/// </summary>
		/// <param name="templateType">The type of template to update (e.g., "QuestionFull").</param>
		/// <param name="data">The data to send.</param>
		public async Task<bool> SendDataToTemplateAsync(string templateType, object data)
		{
			if (!_templateTypeToConnectionId.TryGetValue(templateType, out var connectionId) ||
				!_sockets.TryGetValue(connectionId, out var socket))
			{
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now}  Template of type '{templateType}' is not connected.");
				
				return false;
			}
			var jsonString = JsonConvert.SerializeObject(data);

			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now}  Data Sending to '{templateType}' data: {jsonString}");

			
			var bytes = Encoding.UTF8.GetBytes(jsonString);
			var arraySegment = new ArraySegment<byte>(bytes);


			try
			{
				if (socket.State == WebSocketState.Open)
				{
					await socket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
					_logger.LogInformation($"{DateTime.Now} Data sent successfully to '{templateType}' via WebSocket {socket.GetType}. {socket.ToString()}");					
					return true;
				}
			}
			catch (WebSocketException ex)
			{
				_logger.LogError($"{Environment.NewLine} {ex}, WebSocket error while sending data to '{templateType}'.");
				return false;				
			}
			catch (Exception ex)
			{
				_logger.LogError($"{Environment.NewLine}{ex}, General error while sending data to '{templateType}'.");
				return false;
			}

			_logger.LogWarning($"{Environment.NewLine}Template of type '{templateType}' is not connected or not ready.");
			return false;


		}


		public async Task SendQuestionDataAsync(object data)
		{
			if (_sockets.IsEmpty)
			{
				_logger.LogCritical($"{Environment.NewLine}{DateTime.Now}  No CasparCG template is connected.");
				return;
			}

			var jsonString = JsonConvert.SerializeObject(data);
			var bytes = Encoding.UTF8.GetBytes(jsonString);
			var arraySegment = new ArraySegment<byte>(bytes);

			foreach (var socket in _sockets.Values)
			{
				if (socket.State == WebSocketState.Open)
				{
					await socket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
					_logger.LogInformation($"{DateTime.Now} Question data sent successfully via WebSocket.");
				}
			}
		}
	}
}
