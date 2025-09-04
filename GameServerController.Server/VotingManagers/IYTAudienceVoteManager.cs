using GameController.Shared.Models;

namespace GameController.Server.VotingManagers
{
	public interface IYTAudienceVoteManager
	{
		Task StartNewQuestion(int questionId, string questionText);
		Task ProcessVoteAsync(AudienceMember member, string accessToken, string liveChatId, string message);
		AudienceQuestionData GetCurrentQuestionData();
		Task KickUserAsync(string authorChannelId); 
		bool IsUserKicked(string authorChannelId); 
	}
}
