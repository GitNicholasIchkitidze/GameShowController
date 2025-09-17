using CasparCg.AmcpClient.Commands.Query.Common;
using GameController.Server.Hubs;
using GameController.Shared.Models;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace GameController.Server.Services
{
	public class CasparService : ICasparService, IDisposable
	{
		private readonly TcpClient _client;
		private readonly NetworkStream _stream;
		private readonly object _lock = new();
		private readonly ILogger<CasparService> _logger;

		public CasparService(ILogger<CasparService> logger, string host = "127.0.0.1", int port = 5250)
		{
			_logger = logger;
			_client = new TcpClient();
			try
			{
				_client.Connect(host, port);
				_stream = _client.GetStream();
			}
			catch (Exception ex)
			{
				_logger.LogError($"{Environment.NewLine} Caspar Is not working ");
			}

			
			
		}

		public async Task<OperationResult> SendCommand(string cmd)
		{
			var res = new OperationResult(true);
			try
			{
				
				if (_stream == null)
				{
					res.SetError("გრაფიკის სერვერი არ არის კავშირზე.");
					return res;
				}

				byte[] buffer = Encoding.UTF8.GetBytes(cmd + "\r\n");
				lock (_lock)
				{
					//_stream.Write(buffer, 0, buffer.Length);
					_stream.WriteAsync(buffer, 0, buffer.Length);
				}
				res.Message = "Command sent successfully";
			}
			catch (Exception ex)
			{
				res.SetError($"Failed to send command to CasparCG server {cmd}, error: {ex.Message} ");				
			}			//await Task.CompletedTask;

			return res;
		}


		public Task PlayClip(int channel, int layer, string templateName)
		{
			//string json = JsonSerializer.Serialize(data);
			return SendCommand($"PLAY {channel}-{layer} \"{templateName}\" CUT 1 Linear RIGHT");
		}

		public Task PlayTemplate(int channel, int layer, string templateName, object data)
		{
			string json = JsonSerializer.Serialize(data);			
			return SendCommand($"CG {channel}-{layer} ADD 1 \"{templateName}\" 1 \"{json}\"");
		}

		public Task<OperationResult> LoadTemplate(string templateName, int channel, int layer, int layerCg, bool autoPlay, object? data)
		{
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Going to Load template '{templateName}' {channel}-{layer}");
			string json = JsonSerializer.Serialize(data);
			return SendCommand($"CG {channel}-{layer} ADD {layerCg} {templateName}  0");
			

		}

		public Task StopTemplate(int channel, int layer)
		{
			return SendCommand($"CG {channel}-{layer} STOP 1");
		}

		public void Dispose()
		{
			_stream?.Dispose();
			_client?.Dispose();
		}

		public Task UpdateTemplate(int channel, int layer, int layerCg, bool autoPlay, object? data)
		{
			string json = System.Text.Json.JsonSerializer.Serialize(data);
			
			return SendCommand($"CG {channel}-{layer} UPDATE 1 \"{json}\"");


			
		}

		public Task InvokeTemplate(int channel, int layer, int layerCg, string methodeName, object? data)
		{
			string json = System.Text.Json.JsonSerializer.Serialize(data);
			//json = json.Replace('"', '\'');
			//json = $"\'{json}\'";
			var command = $"CG {channel}-{layer} INVOKE {layerCg} \"{methodeName}({json})\"";
			return SendCommand(command);			
		}

		public string CreateTemplateData(object data)
		{
			// 1. Serialize the C# object to a valid JSON string.
			var jsonString = JsonConvert.SerializeObject(data);

			// 2. Wrap the JSON string in the <templateData> XML tag.
			// We use a CDATA section to prevent XML parsers from misinterpreting the JSON characters.
			var templateDataXml = new XElement("templateData", new XCData(jsonString));

			// 3. Return the full XML string.
			return templateDataXml.ToString(SaveOptions.DisableFormatting);
		}

		public Task UnLoadTemplate(string templateName)
		{
			throw new NotImplementedException();
		}

		public Task ClearChannel(int channel)
		{
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Going to Clear Channel {channel}");
			//string json = JsonSerializer.Serialize(data);
			return SendCommand($"CLEAR {channel}");
		}

		public Task ClearChannelLayer(int channel, int layer)
		{
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Going to Clear Channel {channel}");
			//string json = JsonSerializer.Serialize(data);
			return SendCommand($"CLEAR {channel}-{layer}");
		}

	}
}
