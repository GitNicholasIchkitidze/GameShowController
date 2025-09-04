using GameController.Shared.Enums;
using GameController.Shared.Models.YouTube;
using GameController.YouTubeService.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data; 
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GameController.YouTubeService.Worker
{
	public class YouTubeChatWorker : BackgroundService, IYouTubeChatWorker
	{
		private readonly IConfiguration _configuration;
		private readonly ILogger<YouTubeChatWorker> _logger;
		private readonly ISignalRClient _signalRClient;

		private readonly IYouTubeService _youTubeService;
		private string _liveChatId = string.Empty;
		private string _nextPageToken = string.Empty;
		private readonly string _videoId;
		private readonly int _delay;
		private bool _isVotingModeActive = false;
		private DateTime? _votingStartTime;
		private readonly ConcurrentDictionary<string, string> _votedUsers = new ConcurrentDictionary<string, string>();
		private readonly string[] _validAnswers = { "A", "B", "C", "D" };
		private string _lastVoterName = string.Empty;

		private readonly ConcurrentDictionary<string, DateTime> _channelPublishedDates = new();
		private readonly int _minimumChannelAgeDays;


		private readonly ConcurrentQueue<YouTubeChatMessage> _chatMessages = new();

		public YouTubeChatWorker(ILogger<YouTubeChatWorker> logger, ISignalRClient signalRClient, IYouTubeService youTubeService, IConfiguration configuration)
		{
			_logger = logger;
			_signalRClient = signalRClient;
			_youTubeService = youTubeService ?? throw new ArgumentNullException(nameof(youTubeService));
			_configuration = configuration;
			_videoId = _configuration["YouTubeVideoId"];
			_delay = _configuration.GetValue<int>("YouTubeListRequestDelay");

			_signalRClient.VotingStateChanged += OnVotingStateChanged;
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} >>> YouTubeChatWorker initialized <<<");

		}

		

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} YouTubeChatWorker started.");

			try
			{
				await _signalRClient.ConnectWithRetryAsync();
				_liveChatId = await _youTubeService.GetLiveChatIdAsync(_videoId);
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Successfully retrieved live chat ID: {_liveChatId}");
			}
			catch (Exception ex)
			{
				_logger.LogError($"{Environment.NewLine}{DateTime.Now} Failed to connect to SignalR hub or get live chat ID. {ex}");
			}


			if (string.IsNullOrEmpty(_liveChatId))
			{
				_logger.LogError($"{Environment.NewLine}{DateTime.Now} Not Valid LiveChatId form videoID {_videoId}");
				return;
			}

			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					if (_isVotingModeActive)
					{
						var wasRegisteredAnswer = await FetchAndProcessChatMessages(stoppingToken);
						if (wasRegisteredAnswer)
							await SendLiveVoteUpdateAsync(_chatMessages.LastOrDefault());
						
					}
				}
				catch (Exception ex)
				{
					_logger.LogError($"{Environment.NewLine}{DateTime.Now} An error occurred during chat monitoring. {ex}");
				}
				await Task.Delay(_delay, stoppingToken);
			}

			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} YouTubeChatWorker stopped.");
		}

		public void SetupSignalREventHandlers()
		{
			_signalRClient.VotingStateChanged += OnVotingStateChanged;
		}

		

		public async Task SendVotingStartedChatMessage()
		{
			var message = new ChatMessage
			{
				Type = ChatMessageType.System,
				Content = "ხმის მიცემა ჩართულია. გთხოვთ მიუთითოთ პასუხი კომენტარებში",
				UserId = "system",
				UserName = "სისტემა",
				Timestamp = DateTime.Now
			};

			await _signalRClient.SendChatMessageAsync(message);
		}

		public async Task<bool> FetchAndProcessChatMessages(CancellationToken stoppingToken)
		{
			
			var res = false;
			_lastVoterName = string.Empty;
			var result = await _youTubeService.GetLiveChatMessagesAsync(_liveChatId, _nextPageToken);
			if (result != null)
			{
				if (result.Items != null && result.Items.Count > 0)
				{
					foreach (var item in result.Items)
					{
						try
						{
							if (DateTime.TryParse(item.Snippet?.PublishedAtRaw, out var publishedAt) && publishedAt >= _votingStartTime)

							{
								var msg = new YouTubeChatMessage
								{
									Id = item.Id,
									AuthorChannelId = item.AuthorDetails?.ChannelId,
									UserName = item.AuthorDetails?.DisplayName,
									MessageText = item.Snippet?.DisplayMessage,
									PublishedAt = item.Snippet?.PublishedAtRaw,
									ProfileImageUrl = item.AuthorDetails?.ProfileImageUrl

								};

								var isValidChannel = true; // აქ უნდა გაკეტდეს რომ ახალი შექმნილი ეკაუნტიდან არ მივიღოთ მესიჯები  await IsChannelOldEnoughAsync(msg.AuthorChannelId);

								if (isValidChannel && Array.Exists(_validAnswers, answer => answer.Equals(msg.MessageText?.Trim().ToUpper(), StringComparison.OrdinalIgnoreCase)) )
								{
									if (!string.IsNullOrEmpty(msg.AuthorChannelId) && !string.IsNullOrEmpty(msg.MessageText) && !string.IsNullOrEmpty(msg.UserName))
									{
										if (_votedUsers.TryAdd(msg.AuthorChannelId, msg.MessageText))
										{
											await _youTubeService.SendLiveChatMessageAsync(_liveChatId, $"@{msg.UserName} თქვენი პასუხი რეგისტრირებულია.");
											res = true;
											_lastVoterName= msg.UserName;
										}
										else
										{
											await _youTubeService.SendLiveChatMessageAsync(_liveChatId, $"@{msg.UserName} თქვენ უკვე გაეცით პასუხი.");
											res = false;
											_lastVoterName = string.Empty;
										}
										//_votedUsers.TryAdd(msg.AuthorChannelId, msg.MessageText);

									}
								}

								_chatMessages.Enqueue(msg);
							}
							
						}
						catch (Exception ex)
						{
							_logger.LogError($"{Environment.NewLine}{DateTime.Now} not valid data. {ex.Message}");

							
						}
						
					}
					_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Received {result.Items.Count} new chat messages. Total messages: {_chatMessages.Count}.");
				}
				_nextPageToken = result.NextPageToken;
			}

			return res;
		}


		public async Task ProcessAndSendVoteResultsAsync()
		{
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Processing and sending vote results.");
			var totalVotes = _votedUsers.Count;
			var voteCounts = new ConcurrentDictionary<string, int>();

			foreach (var vote in _votedUsers.Values)
			{
				voteCounts.AddOrUpdate(vote.Trim().ToUpper(), 1, (key, oldValue) => oldValue + 1);
			}

			var results = new List<VoteResult>();
			foreach (var answer in _validAnswers)
			{
				var count = voteCounts.TryGetValue(answer, out var value) ? value : 0;
				var percentage = totalVotes > 0 ? (double)count / totalVotes * 100 : 0;
				results.Add(new VoteResult
				{
					Answer = answer,
					Count = count,
					Percentage = percentage
				});
			}

			var voteResultsMessage = new VoteResultsMessage
			{
				QuestionId = "your_question_id", // This needs to be set dynamically
				Results = results,
				TotalVotes = totalVotes
			};

			//await _signalRClient.SendVoteResultsAsync(voteResultsMessage);
			//_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Sent vote results to SignalR hub. Total votes: {totalVotes}.");
		}
		public async void OnVotingStateChanged(VoteRequestMessage message)
		{
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Received vote request: {message.QuestionId} with options: IsVotingActive {message.IsVotingActive})");
			_isVotingModeActive = message.IsVotingActive;

			if (_isVotingModeActive)
			{
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Voting mode is now active. Monitoring chat for votes.");
				_votingStartTime = DateTime.Now;//.UtcNow;

				// Clear old messages and tokens when voting starts
				while (_chatMessages.TryDequeue(out _)) { }
				_nextPageToken = null;
				_votedUsers.Clear(); // Clear the list of users who have already voted

				await _youTubeService.SendLiveChatMessageAsync(_liveChatId, "📊 ვოტინგი ჩართულია");
				
				//await SendVotingStartedChatMessage();
			}
			else
			{

				var results = GetVoteCurrentResults();
				var chatMessageBuilder = new StringBuilder();

				chatMessageBuilder.AppendLine("📊 ვოტინგი დასრულდა!");

				chatMessageBuilder.AppendLine($"🗳️ სულ დარეგისტრირდა {results.Sum(x=> x.Count)} პასუხი:");

				foreach (var result in results)
				{
					chatMessageBuilder.AppendLine($"   ✔️ {result.Answer}: {result.Count} ({result.Percentage:F2}%)");
				}

				chatMessageBuilder.AppendLine("");
				chatMessageBuilder.AppendLine("🙏 გმადლობთ მონაწილეობისთვის!");
				await _youTubeService.SendLiveChatMessageAsync(_liveChatId, chatMessageBuilder.ToString());

				
				var totalVotes = _votedUsers.Count;

				var voteResultsMessage = new VoteResultsMessage
				{
					QuestionId = "your_question_id", // This needs to be set dynamically
					Results = results,
					TotalVotes = totalVotes,
					VoterName = _lastVoterName
				};

				await _signalRClient.SendVoteResultsAsync(voteResultsMessage);

				//await _youTubeService.SendLiveChatMessageAsync(_liveChatId, "ვოტინგი გამოირთო");
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Voting mode is now inactive. Stopping chat monitoring for votes.");
			}
		}


		
		




		public List<VoteResult> GetVoteCurrentResults()
		{
			_logger.LogInformation("Calculating vote results.");
			var totalVotes = _votedUsers.Count;
			var voteCounts = new ConcurrentDictionary<string, int>();

			foreach (var vote in _votedUsers.Values)
			{
				voteCounts.AddOrUpdate(vote.Trim().ToUpper(), 1, (key, oldValue) => oldValue + 1);
			}

			var results = new List<VoteResult>();
			foreach (var answer in _validAnswers)
			{
				var count = voteCounts.TryGetValue(answer, out var value) ? value : 0;
				var percentage = totalVotes > 0 ? (double)count / totalVotes * 100 : 0;
				results.Add(new VoteResult
				{
					Answer = answer,
					Count = count,
					Percentage = percentage
				});
			}
			return results;
		}


		public async Task SendLiveVoteUpdateAsync(YouTubeChatMessage? lastMessage)
		{
			var results = GetVoteCurrentResults();
			var totalVotes = _votedUsers.Count;

			var voteResultsMessage = new VoteResultsMessage
			{
				QuestionId = "your_question_id", // This needs to be set dynamically
				Results = results,
				TotalVotes = totalVotes,
				VoterName = _lastVoterName,
				ProfileImageUrl = lastMessage?.ProfileImageUrl

			};

			await _signalRClient.SendVoteResultsAsync(voteResultsMessage);
			//_logger.LogInformation($"Sent live vote update to SignalR hub. Total votes: {totalVotes}.");
		}



		private async Task<bool> IsChannelOldEnoughAsync(string? channelId)
		{
			if (string.IsNullOrEmpty(channelId))
			{
				return false;
			}

			if (_channelPublishedDates.TryGetValue(channelId, out var publishedDate))
			{
				return publishedDate.AddDays(_minimumChannelAgeDays) <= DateTime.UtcNow;
			}

			try
			{
				var channel = await _youTubeService.GetChannelDetailsAsync(channelId);
				if (channel?.Snippet?.PublishedAt != null)
				{
					publishedDate = channel.Snippet.PublishedAt.GetValueOrDefault();
					_channelPublishedDates.TryAdd(channelId, publishedDate);
					return publishedDate.AddDays(_minimumChannelAgeDays) <= DateTime.UtcNow;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Failed to get channel details for {channelId}. Assuming channel is not old enough.");
			}

			return false;
		}

	}
}
