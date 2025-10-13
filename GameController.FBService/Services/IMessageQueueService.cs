namespace GameController.FBService.Services
{
	public interface IMessageQueueService
	{
		// Puts the raw Facebook message data into the queue for async processing
		Task EnqueueMessageAsync(string messagePayload);
		IAsyncEnumerable<string> GetMessagesAsync(CancellationToken cancellationToken);

	}
}
