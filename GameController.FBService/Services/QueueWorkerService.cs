using GameController.FBService.Extensions;
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
			_logger.LogInformationWithCaller("Queue Worker Service is running and listening.");

			// The IMessageQueueService is registered as Singleton, so we can resolve it once
			using var scope = _serviceProvider.CreateScope();
			var queueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();

			// ** THIS IS THE CRITICAL CONSUMPTION LOOP **
			await foreach (var rawPayload in queueService.GetMessagesAsync(stoppingToken))
			{
				if (stoppingToken.IsCancellationRequested) break;

				// Log showing the message has been successfully retrieved from the channel
				_logger.LogInformationWithCaller($"Worker received message payload: {rawPayload.Length} bytes. Starting processing...");

				// 3. Create a scope for the WebhookProcessorService (which uses scoped services like ApplicationDbContext)
				using (var processingScope = _serviceProvider.CreateScope())
				{
					

					try
					{
						var processor = processingScope.ServiceProvider.GetRequiredService<IWebhookProcessorService>();
						// 4. Pass the payload to the business logic handler
						var res =await processor.ProcessWebhookMessageAsync(rawPayload);
						if (res.Result)
							_logger.LogInformationWithCaller("Message processing completed successfully.");
						else
							_logger.LogErrorWithCaller($"Message processing failed. Error: {res.Message}");

					}
					catch (Exception ex)
					{
						// Handle errors during processing.
						_logger.LogErrorWithCaller( $"Failed to process message from queue. Payload size: {rawPayload.Length}, {ex}");
					}
				}
			}

			_logger.LogInformationWithCaller("Queue Worker Service has stopped.");
		}

		private string GetMessageFromQueue()
		{
			// In a real app, this logic is specific to your queue technology (RabbitMQ, Service Bus).
			// For now, it simply simulates receiving one message and immediately returning it.
			return null;
		}
	}
}
