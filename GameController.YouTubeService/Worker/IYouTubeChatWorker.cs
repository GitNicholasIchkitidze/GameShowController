using GameController.Shared.Models.YouTube;

namespace GameController.YouTubeService.Worker
{
	public interface IYouTubeChatWorker : IHostedService
	{
		// Add methods and properties here for controlling the YouTube chat service
		// For example:
		// Task StartMonitoringChatAsync(CancellationToken stoppingToken);
		// Task StopMonitoringChatAsync();
		void SetupSignalREventHandlers();
		Task SendVotingStartedChatMessage();
		Task<bool> FetchAndProcessChatMessages(CancellationToken stoppingToken);

		Task ProcessAndSendVoteResultsAsync();
		void OnVotingStateChanged(VoteRequestMessage message);
		List<VoteResult> GetVoteCurrentResults();

		Task SendLiveVoteUpdateAsync(YouTubeChatMessage? lastMessage);
		
	}
}
