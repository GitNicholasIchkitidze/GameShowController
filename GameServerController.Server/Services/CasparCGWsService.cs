// CasparCGWsService.cs
using GameController.Server.Hubs;
using GameController.Shared.Enums;
using GameController.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace GameController.Server.Services
{
    public class CasparCGWsService : ICasparCGWsService
    {
        private readonly ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();
        private readonly ConcurrentDictionary<string, string> _connectionIdToTemplateType = new ConcurrentDictionary<string, string>();
        private readonly ConcurrentDictionary<string, string> _templateTypeToConnectionId = new ConcurrentDictionary<string, string>();

        private readonly ILogger<CasparCGWsService> _logger;
		private readonly IHubContext<GameHub> _hubContext;
		public CasparCGWsService(ILogger<CasparCGWsService> logger, IHubContext<GameHub> hubContext)
        {
            _logger = logger;
			_hubContext = hubContext; 
		}
        public void AddConnection(WebSocket socket)
        {
            var connectionId = Guid.NewGuid().ToString();
            _ = _sockets.TryAdd(connectionId, socket);

            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} WebSocket connection established with ID: {connectionId}");

            // Start listening for a registration message from the client
            _ = ReceiveRegistrationAsync(socket, connectionId);
        }


		public async Task<OperationResult> ReceiveRegistrationAsync_(WebSocket socket, string connectionId)
		{
			var result = new OperationResult(true);
			_logger.LogInformation($@"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} ReceiveRegistrationAsync ConnectionID: {connectionId}");

			try
			{
				var buffer = new byte[1024 * 4];
				WebSocketReceiveResult receiveResult;

				// Loop to wait for the initial registration message
				while (socket.State == WebSocketState.Open)
				{
					receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

					if (receiveResult.MessageType == WebSocketMessageType.Close)
					{
						await RemoveConnectionAsync(connectionId);
						
						result.SetError("Connection Closed before Registration, კავშირი დაიხურა რეგისტრაციამდე.");
						return result;
					}

					if (receiveResult.MessageType == WebSocketMessageType.Text)
					{
						//var jsonString = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);

						var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
						// დინამიური ობიექტის გამოყენება მონაცემების გასაანალიზებლად
						//dynamic registration = JsonConvert.DeserializeObject(jsonString);

						var data = JsonConvert.DeserializeObject<dynamic>(message);
						if (data.type == "register" && data.templateName != null)
							//if (registration != null && registration?.Type != null && registration?.TemplateName != null &&
							//registration?.Type.ToString().Equals("register", StringComparison.OrdinalIgnoreCase) &&
							//!string.IsNullOrEmpty(registration?.TemplateName.ToString()))
						{
							var templateName = data.TemplateName.ToString();

							// ძველი რეგისტრაციის გასუფთავება (გადატვირთვის შემთხვევაში)
							if (_connectionIdToTemplateType.TryGetValue(connectionId, out var oldTemplateType))
							{
								_templateTypeToConnectionId.TryRemove(oldTemplateType, out _);
								_connectionIdToTemplateType.TryRemove(connectionId, out _);
								_logger.LogWarning($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} ReRegistration with ID {connectionId} Removed (Old ID: {oldTemplateType}).");
							}

							// ახალი ასახვის დამატება
							if (_templateTypeToConnectionId.TryAdd(templateName, connectionId) &&
								_connectionIdToTemplateType.TryAdd(connectionId, templateName))
							{
								_logger.LogInformation($@"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} შაბლონი '{templateName}' წარმატებით დარეგისტრირდა ID-ით: {connectionId}");

								// *** NEW: SignalR კლიენტების (ოპერატორის) გაფრთხილება წარმატებული რეგისტრაციის შესახებ ***
								await _hubContext.Clients.All.SendAsync("CGTemplateStatusUpdate", new TemplateRegistrationModel
								{
									TemplateName = templateName,
									IsRegistered = true,
									ConnectionId = connectionId
								});
								_logger.LogInformation($@"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Updated RegistrationStatus with SignalR clients.");

								// რეგისტრაციის შემდეგ, გამოვდივართ რეგისტრაციდან
								break;
							}
							else
							{
								_logger.LogError($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} ვერ მოხერხდა შაბლონის ტიპის '{templateName}' Not Added in Dictionary.");
								
								result.SetError($"'Template {templateName}' Not Registered.");
								await RemoveConnectionAsync(connectionId);
								return result;
							}
						}
					}
				}

				// წარმატებული რეგისტრაციის დაბრუნება
				return result;
			}
			catch (WebSocketException ex)
			{
				_logger.LogError($@"{DateTime.Now} WebSocket Error while registration {connectionId}: {ex.Message}");
				
				result.SetError("WebSocket კომუნიკაციის შეცდომა რეგისტრაციისას.");
				await RemoveConnectionAsync(connectionId);
			}
			catch (Exception ex)
			{
				_logger.LogError($@"{DateTime.Now} Error WebSocket while connection {connectionId}: {ex.Message}");
				
				result.SetError("შიდა სერვერის შეცდომა რეგისტრაციისას.");
				await RemoveConnectionAsync(connectionId);
			}

			return result;
		}
		public async Task<OperationResult> ReceiveRegistrationAsync(WebSocket socket, string connectionId)
        {
            var result = new OperationResult(false);
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")}  Entered in ReceiveRegistrationAsync:' connectionId: {connectionId}' socketState: {socket.State.ToString()}");

            var buffer = new byte[1024 * 4];
            var receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

			if (receiveResult.MessageType == WebSocketMessageType.Close)
			{
				await RemoveConnectionAsync(connectionId);
				
				result.SetError($"კავშირი დაიხურა რეგისტრაციამდე. {connectionId}") ;
				return result;
			}

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
                        _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")}  Template '{templateName}' successfully registered.{Environment.NewLine}");
						result.SetSuccess($"Template '{templateName}' successfully registered.");
                        result.Results = templateName;



						// *** NEW: SignalR კლიენტების (ოპერატორის) გაფრთხილება წარმატებული რეგისტრაციის შესახებ ***
						
						await _hubContext.Clients.All.SendAsync("CGTemplateStatusUpdate", new TemplateRegistrationModel
						{
							TemplateName = templateName,
							IsRegistered = true,
							ConnectionId = connectionId
						});
						_logger.LogInformation($@"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Updated RegistrationStatus with SignalR clients.");


					}
				}
                catch (Exception ex)
                {
                    _logger.LogError($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")}  Failed to parse registration message from client {connectionId}: {ex.Message}");
					result.SetError($"Failed to parse registration message from client {connectionId}: {ex.Message}");

				}
            }
            return result;
		}

        /// <summary>
        /// Sends data to a specific template type.
        /// </summary>
        /// <param name="templateType">The type of template to update (e.g., "QuestionFull").</param>
        /// <param name="data">The data to send.</param>
        public async Task<bool> SendDataToTemplateAsync_(string templateType, object data)
        {
            if (!_templateTypeToConnectionId.TryGetValue(templateType, out var connectionId) ||
                !_sockets.TryGetValue(connectionId, out var socket))
            {
                _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")}  Template of type '{templateType}' is not connected.");

                return false;
            }
            var jsonString = JsonConvert.SerializeObject(data);

            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")}  Data Sending to '{templateType}' data: {jsonString}");


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

		public async Task<bool> SendDataToTemplateAsync(string templateType, object data)
		{
			// Implementation to send data
			if (_templateTypeToConnectionId.TryGetValue(templateType, out var connectionId))
			{
				if (_sockets.TryGetValue(connectionId, out var socket) && socket.State == WebSocketState.Open)
				{
					try
					{
						var jsonString = JsonConvert.SerializeObject(data);
						var bytes = Encoding.UTF8.GetBytes(jsonString);
						var arraySegment = new ArraySegment<byte>(bytes);

						await socket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
						_logger.LogInformation($@"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Data Sent to '{templateType}' via WebSocket.");
						return true;
					}
					catch (WebSocketException ex)
					{
						_logger.LogError($@"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} WebSocket შეცდომა მონაცემების გაგზავნისას '{templateType}'. {ex}");
						_ = RemoveConnectionAsync(connectionId); // ცუდი კავშირის მოხსნა
						return false;
					}
					catch (Exception ex)
					{
						_logger.LogError($@"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")}, ზოგადი შეცდომა მონაცემების გაგზავნისას '{templateType}'. {ex}");
						_ = RemoveConnectionAsync(connectionId); // ცუდი კავშირის მოხსნა
						return false;
					}
				}
			}

			_logger.LogWarning($@"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} შაბლონი ტიპის '{templateType}' არ არის დაკავშირებული ან მზად.");
			return false;


		}

		public async Task SendQuestionDataAsync_(object data)
        {
            if (_sockets.IsEmpty)
            {
                _logger.LogCritical($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")}  No CasparCG template is connected.");
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

		public async Task SendQuestionDataAsync(object data)
		{
			if (_sockets.IsEmpty)
			{
				_logger.LogCritical($@"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} CasparCG შაბლონი არ არის დაკავშირებული.");
				return;
			}

			var jsonString = JsonConvert.SerializeObject(data);
			var bytes = Encoding.UTF8.GetBytes(jsonString);
			var arraySegment = new ArraySegment<byte>(bytes);

			foreach (var socketEntry in _sockets.ToList())
			{
				var connectionId = socketEntry.Key;
				var socket = socketEntry.Value;
				if (socket.State == WebSocketState.Open)
				{
					try
					{
						await socket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
						_logger.LogInformation($@"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} შეკითხვის მონაცემები წარმატებით გაეგზავნა WebSocket-ის საშუალებით.");
					}
					catch (Exception ex)
					{
						_logger.LogError($"მონაცემების გაგზავნის შეცდომა კავშირისთვის {connectionId}. კავშირის მოხსნა. {ex}");
						await RemoveConnectionAsync(connectionId);
					}
				}
			}
		}


		// დამხმარე მეთოდი კავშირის მოსაშორებლად და Hub-ის გასაფრთხილებლად
		public async Task RemoveConnectionAsync(string connectionId)
		{
			if (_sockets.TryRemove(connectionId, out var socket))
			{
				_logger.LogInformation($@"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} WebSocket კავშირი ID-ით: {connectionId} მოიხსნა.");

				// შაბლონის ასახვის მოხსნა
				if (_connectionIdToTemplateType.TryRemove(connectionId, out var templateType))
				{
					_templateTypeToConnectionId.TryRemove(templateType, out _);
					_logger.LogInformation($@"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} შაბლონის ასახვა '{templateType}' მოიხსნა.");

					// *** NEW: SignalR კლიენტების (ოპერატორის) გაფრთხილება რეგისტრაციის მოხსნის შესახებ ***
					await _hubContext.Clients.All.SendAsync("CGTemplateStatusUpdate", new
					{
						TemplateName = templateType,
						IsRegistered = false,
						ConnectionId = connectionId
					});
				}

				if (socket.State != WebSocketState.Closed && socket.State != WebSocketState.Aborted)
				{
					try
					{
						await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
					}
					catch (Exception ex)
					{
						_logger.LogError($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")}, კავშირის დახურვის შეცდომა {connectionId}.{ex}");
					}
				}
			}
		}

		public List<string> GetRegisteredTemplates()
		{
			return _templateTypeToConnectionId.Keys.ToList();
		}
	}
}
