
using GameController.FBService.Extensions;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Channels;

namespace GameController.FBService.Services
{
	public class MessageQueueService : IMessageQueueService
	{
		private readonly ILogger<MessageQueueService> _logger;
		private readonly IAppMetrics _metrics;                 // ADDED
		private readonly Channel<string> _channel;

		// NOTE: In a real app, this would include the connection/client for RabbitMQ/Service Bus.


		// ADDED: counters for monitoring
		private long _depth;
		private long _dropped;
		private long _peakDepth;
		

		public long CurrentDepth => Interlocked.Read(ref _depth);
		public long DroppedCount => Interlocked.Read(ref _dropped);
		private bool IsNearFull => CurrentDepth >= Capacity - 200;
		public int Capacity { get; }

		bool IMessageQueueService.IsNearFull => IsNearFull;



		public MessageQueueService(ILogger<MessageQueueService> logger, IConfiguration configuration, IAppMetrics metrics)
		{
			_logger = logger;
			_metrics = metrics;

			//_channel = Channel.CreateUnbounded<string>(options);


			// ADDED (2025-12): 
			var configuredCapacity = configuration.GetValue<int>("Queue:Capacity", 10000);
			if (configuredCapacity < 1) configuredCapacity = 10000;
			Capacity = configuredCapacity;         

			var options = new BoundedChannelOptions(Capacity)
			{
				SingleWriter = false,
				SingleReader = false,

				// ✅ When full, DROP new items instead of blocking webhook thread/worker producers
				//FullMode = BoundedChannelFullMode.DropWrite
				FullMode = BoundedChannelFullMode.Wait

			};
			_channel = Channel.CreateBounded<string>(options);
			_logger.LogInformationWithCaller($"[QUEUE] BOUNDED channel created. Capacity={Capacity}, FullMode=DropWrite");

		}

		public long ConsumePeakDepth()
		{
			var current = CurrentDepth;
			return Interlocked.Exchange(ref _peakDepth, current);
		}
		public void UpdatePeakDepth(long depthNow)
		{
			while (true)
			{
				var prev = Interlocked.Read(ref _peakDepth);
				if (depthNow <= prev) return;

				if (Interlocked.CompareExchange(ref _peakDepth, depthNow, prev) == prev)
					return;
			}
		}



		
		public Task EnqueueMessageAsync(string messagePayload)
		{
			_ = TryEnqueueMessage(messagePayload);
			return Task.CompletedTask;
		}

		


		public async IAsyncEnumerable<string> GetMessagesAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken) // ADDED (2025-12): 
		{
			await foreach (var item in _channel.Reader.ReadAllAsync(cancellationToken))
			{
				Interlocked.Decrement(ref _depth);
				_metrics.IncDequeued(); // ADDED

				yield return item;
			}
		}

		public bool TryEnqueueMessage(string messagePayload)
		{
			if (messagePayload == null || string.IsNullOrWhiteSpace(messagePayload))
			{
				Interlocked.Increment(ref _dropped);
				_metrics.IncEnqueueDropped(); // ADDED

				return false;
			}

			// CHANGED: Non-blocking enqueue (critical for webhook fast ACK).
			if (_channel.Writer.TryWrite(messagePayload))
			{
				var depthNow = Interlocked.Increment(ref _depth);
				UpdatePeakDepth(depthNow);

				//Interlocked.Increment(ref _depth);
				_metrics.IncEnqueueOk(); // ADDED

				


				// CHANGED: Avoid INFO log on every message (kills throughput).
				// Enable Debug when you need to inspect queue behavior.
				_logger.LogDebug("[QUEUE] Enqueued payload. Bytes={Size}. Depth={Depth}", messagePayload.Length, CurrentDepth);

				return true;
			}

			Interlocked.Increment(ref _dropped);
			_metrics.IncEnqueueDropped(); // ADDED



			
			// NOTE: This is a *signal* that you're in peak mode / queue is undersized.
			//_logger.LogWarningWithCaller($"[QUEUE] Queue is full (capacity={Capacity}). Dropping payload. DroppedCount={DroppedCount}");
			return false;
		}

	}
}
