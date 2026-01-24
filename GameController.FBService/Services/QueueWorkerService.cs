using GameController.FBService.Extensions;
using GameController.FBService.Models;
using GameController.FBService.Services;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace GameController.FBService.Services
{
	public class QueueWorkerService : BackgroundService
	{
		private readonly ILogger<QueueWorkerService> _logger;
		private readonly IServiceProvider _serviceProvider; // To create scoped services

		private readonly IMessageQueueService _queueService;		// ADDED (2025-12): 
		private readonly IServiceScopeFactory _scopeFactory;        // ADDED (2025-12): 
		private readonly IAppMetrics _metrics;                       // ADDED


		// ADDED: Multi-worker support
		private readonly int _workerCount;                          // ADDED (2025-12): 

		public QueueWorkerService(ILogger<QueueWorkerService> logger, IServiceProvider serviceProvider,
			IMessageQueueService queueService,
			IServiceScopeFactory scopeFactory,
			IAppMetrics metrics,
			IConfiguration configuration
			)
		{
			_logger = logger;
			_serviceProvider = serviceProvider;
			_metrics = metrics;


			_queueService = queueService;                                           
			_scopeFactory = scopeFactory;                                           

			// CHANGED: Configure worker count (default 8).
			_workerCount = configuration.GetValue<int>("Queue:Workers", 8);         
			if (_workerCount < 1) _workerCount = 1;                                 
		}

		


		




		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformationWithCaller($"Queue Worker Service is running. Workers={_workerCount}, QueueCapacity={_queueService.Capacity}");

			// CHANGED: Start multiple consumers to improve throughput during bursts.
			var tasks = new Task[_workerCount];
			for (int i = 0; i < _workerCount; i++)
			{
				var workerId = i;
				tasks[i] = Task.Run(() => WorkerLoopAsync(workerId, stoppingToken), stoppingToken);
			}

			try
			{
				await Task.WhenAll(tasks);
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				// Graceful shutdown
			}
			finally
			{
				_logger.LogInformationWithCaller("Queue Worker Service has stopped.");
			}
		}

		private async Task WorkerLoopAsync(int workerId, CancellationToken ct)
		{
			_logger.LogInformationWithCaller($"Worker #{workerId} started.");

			long processed = 0;

			await foreach (var rawPayload in _queueService.GetMessagesAsync(ct))
			{
				if (ct.IsCancellationRequested) break;

				_metrics.IncInFlight();

				try
				{
					// IMPORTANT: Create a scope PER message, because IWebhookProcessorService uses scoped dependencies (DbContext, etc.)
					using var scope = _scopeFactory.CreateScope();
					var processor = scope.ServiceProvider.GetRequiredService<IWebhookProcessorService>();

					var res = await processor.ProcessWebhookMessageAsync(rawPayload);

					processed++;

					// CHANGED: Avoid noisy per-message INFO logs.
					if (processed % 500 == 0)
					{
						_logger.LogInformationWithCaller($"Worker #{workerId} processed {processed} messages. QueueDepth={_queueService.CurrentDepth}, Dropped={_queueService.DroppedCount}");
					}

					// If you want to log failures, do it only when Result==false (keeps throughput).
					if (!res.Result)
					{
						//_logger.LogWarningWithCaller($"Worker #{workerId} message processing failed: {res.Message}");
						if (res.Message == "Non-voteStart message ignored.")
							_metrics.IncGarbageMessages();
						else if (res.Message == "Vote denied due to rate limit.")
							_metrics.IncNotInTimeUserMessages();
						else
							_metrics.IncProcessedFailed();
					}
					else
					{
						_metrics.IncProcessedOk(); // ADDED
					}
				}
				catch (OperationCanceledException) when (ct.IsCancellationRequested)
				{
					break;
				}
				catch (Exception ex)
				{
					_metrics.IncProcessedFailed(); // ADDED

					// Never allow a single bad payload to kill the worker loop.
					_logger.LogErrorWithCaller($"Worker #{workerId} failed to process message. PayloadBytes={(rawPayload?.Length ?? 0)}. {ex}");
				}
				finally
				{
					_metrics.DecInFlight(); // ✅ ADDED (finish processing)
				}
			}

			_logger.LogInformationWithCaller($"Worker #{workerId} stopped. TotalProcessed={processed}");
		}







		private string GetMessageFromQueue()
		{
			// In a real app, this logic is specific to your queue technology (RabbitMQ, Service Bus).
			// For now, it simply simulates receiving one message and immediately returning it.
			return null;
		}
	}
}
