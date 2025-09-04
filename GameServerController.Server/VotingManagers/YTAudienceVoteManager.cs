using GameController.Server.Hubs;

using GameController.Server.VotingManagers;
using GameController.Server.VotingServices;
using GameController.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Linq;

namespace GameController.Server.Services
{
	public class YTAudienceVoteManager : IYTAudienceVoteManager
	{
		private readonly IHubContext<GameHub> _hubContext;
		private readonly ConcurrentDictionary<string, string> _votedAudienceMembers;
		private readonly ConcurrentDictionary<string, bool> _kickedAudienceMembers; 



		private AudienceQuestionData _currentQuestionData;
		private readonly IYouTubeChatService _youtubeChatService;
		private string _liveChatId;

		public YTAudienceVoteManager(IHubContext<GameHub> hubContext, IYouTubeChatService youtubeChatService)
		{
			_hubContext = hubContext;
			_votedAudienceMembers = new ConcurrentDictionary<string, string>();
			_kickedAudienceMembers = new ConcurrentDictionary<string, bool>(); 
			_youtubeChatService = youtubeChatService; // ინექცია

		}

		public async Task StartNewQuestion(int questionId, string questionText)
		{
			_votedAudienceMembers.Clear();
			//_kickedAudienceMembers.Clear(); // დავამატოთ გამოგდებული მომხმარებლების გასუფთავება

			_currentQuestionData = new AudienceQuestionData
			{
				QuestionId = questionId,
				QuestionText = questionText,
				AnswerCounts = new ConcurrentDictionary<string, int>(),
				AnswerPercentages = new Dictionary<string, double>()
			};
		}


		public async Task ProcessVoteAsync(AudienceMember member, string accessToken, string liveChatId, string message)
		{
			if (_currentQuestionData == null)
			{
				return;
			}

			_liveChatId = liveChatId;

			var uniqueKey = $"{member.Platform}_{member.AuthorChannelId}";

			// 4) შეამოწმეთ, არის თუ არა მომხმარებელი უკვე ნამყოფი
			if (_votedAudienceMembers.ContainsKey(uniqueKey))
			{
				await _youtubeChatService.PostChatMessageAsync(
					liveChatId,
					$"{member.AuthorName} თქვენ უკვე გაეცით პასუხი.",
					accessToken
				);
				return;
			}

			// 5) შეამოწმეთ, არის თუ არა მომხმარებელი გამოგდებული
			if (IsUserKicked(member.AuthorChannelId))
			{
				await _youtubeChatService.PostChatMessageAsync(
					liveChatId,
					$"მომხმარებელი {member.AuthorName} გაგდებულია თამაშიდან.",
					accessToken
				);
				return;
			}

			// თუ პასუხი დასაშვებია და მომხმარებელი პირველად აძლევს ხმას
			if (_votedAudienceMembers.TryAdd(uniqueKey, member.Answer))
			{
				_currentQuestionData.AnswerCounts.AddOrUpdate(
					member.Answer,
					1,
					(key, oldValue) => oldValue + 1
				);
				_currentQuestionData.TotalVotes++;

				await _youtubeChatService.PostChatMessageAsync(
					liveChatId,
					$"{member.AuthorName} თქვენი პასუხი ({member.Answer}) მიღებულია. მადლობა მონაწილეობისთვის!",
					accessToken
				);

				await RecalculateAndSendResultsAsync();
			}
		}
		public async Task ProcessVoteAsync_(AudienceMember member, string accessToken, string liveChatId, string message)
		{
			if (_currentQuestionData == null)
			{
				// თუ კითხვა არ არის აქტიური, უგულებელყოფთ პასუხს
				// return;
			}

			// ვინახავთ liveChatId-ს, რათა მოგვიანებით გამოვიყენოთ სხვა მეთოდებში
			_liveChatId = liveChatId;

			var uniqueKey = $"{member.Platform}_{member.AuthorChannelId}";
			if (_votedAudienceMembers.TryAdd(uniqueKey, member.Answer))
			{
				////_currentQuestionData.AnswerCounts.AddOrUpdate(
				////	member.Answer,
				////	1,
				////	(key, oldValue) => oldValue + 1
				////);
				////_currentQuestionData.TotalVotes++;

				// აქ ვუგზავნით უკუკავშირს, უკვე დინამიური liveChatId-ის გამოყენებით
				await _youtubeChatService.PostChatMessageAsync(
					_liveChatId,
					$"{member.AuthorName} {message} ({member.Answer}). მადლობა მონაწილეობისთვის!",
					accessToken
				);

				await RecalculateAndSendResultsAsync();
			}
		}

		public AudienceQuestionData? GetCurrentQuestionData()
		{
			return _currentQuestionData;
		}

		private async Task RecalculateAndSendResultsAsync()
		{
			if (_currentQuestionData == null) return;

			var totalVotes = _currentQuestionData.AnswerCounts.Values.Sum();

			var percentages = _currentQuestionData.AnswerCounts.ToDictionary(
				kvp => kvp.Key,
				kvp => totalVotes > 0 ? (double)kvp.Value / totalVotes * 100 : 0
			);

			_currentQuestionData.AnswerPercentages = percentages;

			await _hubContext.Clients.All.SendAsync("UpdateAudienceVotingResults", _currentQuestionData);

		}

		public async Task KickUserAsync(string authorChannelId)
		{
			_kickedAudienceMembers.TryAdd(authorChannelId, true);
		}

		public bool IsUserKicked(string authorChannelId)
		{
			return _kickedAudienceMembers.ContainsKey(authorChannelId);
		}
	}
}