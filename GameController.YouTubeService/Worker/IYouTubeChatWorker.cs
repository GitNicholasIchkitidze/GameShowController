using GameController.Shared.Models.YouTube;

namespace GameController.YouTubeService.Worker
{
	public interface IYouTubeChatWorker : IHostedService
	{



		void SetupSignalREventHandlers();
		Task SendVotingStartedChatMessage();
		Task<bool> FetchAndProcessChatMessages(CancellationToken stoppingToken);

		Task ProcessAndSendVoteResultsAsync();
		void OnVotingStateChanged(VoteRequestMessage message);
		List<VoteResult> GetVoteCurrentResults();

		Task SendLiveVoteUpdateAsync(YouTubeChatMessage? lastMessage);


	}
}
