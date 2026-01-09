namespace GameController.FBService.Services
{
	public interface IMessageQueueService
	{
		// Puts the raw Facebook message data into the queue for async processing
		Task EnqueueMessageAsync(string messagePayload);
		IAsyncEnumerable<string> GetMessagesAsync(CancellationToken cancellationToken);



		// CHANGED (2025-12): Bounded queue + non-blocking enqueue for Webhook fast-ACK.
		// IMPORTANT: Webhook controller MUST NOT await/slow down here. If queue is full, we drop (and count it),
		// because blocking would cause Facebook retries/timeouts.
		bool TryEnqueueMessage(string messagePayload);
		bool IsNearFull { get; }

		// ADDED: Lightweight counters for monitoring / peak detection.

		long CurrentDepth { get; }
		long DroppedCount { get; }
		int Capacity { get; }


		long ConsumePeakDepth();


	}
}
