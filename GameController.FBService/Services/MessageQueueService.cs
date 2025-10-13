
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Channels;

namespace GameController.FBService.Services
{
	public class MessageQueueService : IMessageQueueService
	{
		private readonly ILogger<MessageQueueService> _logger;
		private readonly Channel<string> _channel;

		// NOTE: In a real app, this would include the connection/client for RabbitMQ/Service Bus.

		public MessageQueueService(ILogger<MessageQueueService> logger)
		{
			_logger = logger;
			_channel = Channel.CreateUnbounded<string>();

		}

		public async Task EnqueueMessageAsync(string messagePayload)
		{
			await _channel.Writer.WriteAsync(messagePayload);
			_logger.LogInformation($"[QUEUE] Message Enqueued successfully to internal Channel. Size: {messagePayload.Length} bytes.");


		}

		public IAsyncEnumerable<string> GetMessagesAsync(CancellationToken cancellationToken)
		{
			// Returns the reader for the Worker to consume (Consumer)
			return _channel.Reader.ReadAllAsync(cancellationToken);
		}
	}
}
