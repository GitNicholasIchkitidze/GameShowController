using GameController.FBService.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Text.Json;

namespace GameController.FBService.Services
{
	public class QueueWorkerService : BackgroundService
	{
		private readonly ILogger<QueueWorkerService> _logger;
		private readonly IServiceProvider _serviceProvider; // To create scoped services

		public QueueWorkerService(ILogger<QueueWorkerService> logger, IServiceProvider serviceProvider)
		{
			_logger = logger;
			_serviceProvider = serviceProvider;
		}

		


		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("Queue Worker Service is running and listening.");

			// The IMessageQueueService is registered as Singleton, so we can resolve it once
			using var scope = _serviceProvider.CreateScope();
			var queueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();

			// ** THIS IS THE CRITICAL CONSUMPTION LOOP **
			await foreach (var rawPayload in queueService.GetMessagesAsync(stoppingToken))
			{
				if (stoppingToken.IsCancellationRequested) break;

				// Log showing the message has been successfully retrieved from the channel
				_logger.LogInformation($"Worker received message payload: {rawPayload.Length} bytes. Starting processing...");

				// 3. Create a scope for the WebhookProcessorService (which uses scoped services like ApplicationDbContext)
				using (var processingScope = _serviceProvider.CreateScope())
				{
					

					try
					{
						var processor = processingScope.ServiceProvider.GetRequiredService<IWebhookProcessorService>();
						// 4. Pass the payload to the business logic handler
						await processor.ProcessWebhookMessageAsync(rawPayload);
						_logger.LogInformation("Message processing completed successfully.");

					}
					catch (Exception ex)
					{
						// Handle errors during processing.
						_logger.LogError(ex, $"Failed to process message from queue. Payload size: {rawPayload.Length}");
					}
				}
			}

			_logger.LogInformation("Queue Worker Service has stopped.");
		}

		private string GetMessageFromQueue()
		{
			// In a real app, this logic is specific to your queue technology (RabbitMQ, Service Bus).
			// For now, it simply simulates receiving one message and immediately returning it.
			return null;
		}
	}
}
