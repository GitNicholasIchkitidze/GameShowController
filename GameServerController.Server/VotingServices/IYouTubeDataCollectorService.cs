using GameController.Shared.Models;

namespace GameController.Server.VotingServices
{
	public interface IYouTubeDataCollectorService
	{


		Task<OperationResult> StartCollectingAsync();
		void StopCollecting();
	}
}
