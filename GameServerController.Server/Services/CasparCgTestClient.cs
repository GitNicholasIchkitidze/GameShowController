
using CasparCg.AmcpClient;
using CasparCg.AmcpClient.Commands.Cg;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System;
namespace GameController.Server.Services
{

	public class CasparCgTestClient
	{
		private readonly AmcpConnection _connection;

		public CasparCgTestClient()
		{
			_connection = new AmcpConnection("127.0.0.1", 5250);
		}

		public async Task RunTestAsync()
		{
			try
			{
				await _connection.ConnectAsync();
				Console.WriteLine("Connected to CasparCG.");

				// Create a sample data object
				var data = new
				{
					Question = "ახალი კითხვა",
					QuestionImage = "https://example.com/image.png",
					Answers = new[] { "პასუხი 1", "პასუხი 2", "პასუხი 3" }
				};

				// Serialize the data to a clean JSON string
				var jsonString = JsonConvert.SerializeObject(data);
				Console.WriteLine($"Sending JSON: {jsonString}");

				// The correct command is CG [channel]-[layer] INVOKE [component] "method" "data"
				// We use CG INVOKE because it's the modern way to send JSON data.
				// The command to invoke is "update" and the component is "0" (root).
				var invokeCommand = new CgInvokeCommand(2, 10, 0, "update");

				var result = await invokeCommand.ExecuteAsync(_connection);

				if (result != null)
				{
					Console.WriteLine("Command sent successfully.");
				}
				else
				{
					Console.WriteLine($"Command failed. Status: {"result.ResponseText"}");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"An error occurred: {ex.Message}");
			}
			finally
			{
				_connection.Disconnect();
			}
		}
	}
}
