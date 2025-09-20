
using CasparCg.AmcpClient.Commands.Query.Common;
using GameController.Server.Services;
using GameController.Server.VotingManagers;
using GameController.Server.VotingServices;
using GameController.Shared.Enums;
using GameController.Shared.Models;
using GameController.Shared.Models.Connection;
using GameController.Shared.Models.YouTube;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GameController.Server.Hubs
{
	public class GameHub : Hub
	{
		private readonly ILogger<GameHub> _logger;
		private readonly IConfiguration _configuration;
		private readonly IMidiLightingService _midiLightingService;
		private readonly IGameService _gameService;
		private readonly MidiSettingsModels _midiSettings;


		private readonly List<ClientConfiguration> _registeredClients;
		//private static readonly ConcurrentDictionary<string, Player> _connectedPlayers = new ConcurrentDictionary<string, Player>();
		// Change the declaration of _questionService to non-static and assign it in the constructor
		private readonly IQuestionService _questionService;
		private static List<QuestionModel>? _questions;
		private static QuestionModel? _activeQuestion;

		private static int _answersReceivedCount;
		private static List<string> _activePlayerIds = new List<string>();
		private static CountdownMode _currentCountdownMode;
		private static GameMode _currentGameMode;
		private static int _rapidFireCurrentQuestionIndex = -1;
		private static bool _currentDisableInput;

		private static bool _isRapidFireActive = false;
		//private static bool _isRapidFireActive = false;
		private static string? _operatorConnectionId;
		private static string? _ytVoterManagerConnectionId;
		private static string? _fbVoterManagerConnectionId;
		private static string? _hostConnectionId;
		private bool trackerLog = true;



		//private readonly CasparCGService _casparCGService;
		private readonly CasparCGSettings? _cgSettings;
		private static bool _isLeaderBoardActive = false; // Add this static variable to track state
		private static List<PlayerScore> _finalScores = new List<PlayerScore>();

		private static bool _isCountdownActiveOnCG = false; // To track if the CG countdown is running

		
		private readonly ConcurrentDictionary<string, bool> _isTemplateLoaded = new ConcurrentDictionary<string, bool>();
		private readonly ICasparService _caspar;
		private readonly CasparCGWsService _casparCGWsService;



		private readonly IYTAudienceVoteManager _ytAudienceVoteManager;
		private readonly IYouTubeDataCollectorService _ytdataCollectorService;
		private readonly IYouTubeChatService _youtubeChatService; // 

		public static string? YTaccessToken;
		public static string? YTrefreshToken;
		private readonly IYTOAuthTokenService _ytoauthTokenService;
		public static bool _isYTVotingModeActive = false; // ახალი
														  //private static ConcurrentDictionary<string, bool> _activeYTAudienceIds = new ConcurrentDictionary<string, bool>();


		private static readonly Dictionary<CGTemplateEnums, (int channel, string templateName, int layer, int layerCg)> _cgSettingsMap = new();




		public GameHub(IMidiLightingService midiLightingService, 
			IOptions<MidiSettingsModels> midiSettings, 
			ILogger<GameHub> logger, 
			IConfiguration configuration, 
			IQuestionService questionService,
			ICasparService caspar,
			IGameService gameService,
			CasparCGWsService casparCGWsService,
			IYTAudienceVoteManager audienceVoteManager,
			IYouTubeDataCollectorService ytdataCollectorService,
			IYTOAuthTokenService ytoauthTokenService,
			IYouTubeChatService youtubeChatService

			)
		{
			_logger = logger;
			_configuration = configuration;
			_registeredClients = _configuration.GetSection("RegisteredClients").Get<List<ClientConfiguration>>() ?? new List<ClientConfiguration>();
			_questionService = questionService;
			_caspar = caspar;
			_midiLightingService = midiLightingService;
			_midiSettings = midiSettings.Value;
			_gameService = gameService;

			_ytAudienceVoteManager = audienceVoteManager;
			_ytdataCollectorService = ytdataCollectorService;
			_ytoauthTokenService = ytoauthTokenService;
			_youtubeChatService = youtubeChatService;

			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} >>> GameHub instance created <<<");


			// გამოიწერეთ event-ი და გამოიძახეთ მეთოდი სტატუსის ცვლილებისას
			_midiLightingService.ConnectionStatusChanged += OnMidiConnectionStatusChanged;


			//if (_questions == null)
			//{
			//	_questions = questionService.LoadQuestionsAsync().Result;
			//}


			_cgSettings = _configuration.GetSection("CG").Get<CasparCGSettings>();


			if (_cgSettings != null)
			{
				_cgSettingsMap[CGTemplateEnums.QuestionFull] = (_cgSettings.QuestionFull.Channel, _cgSettings.QuestionFull.TemplateName, _cgSettings.QuestionFull.Layer, _cgSettings.QuestionFull.LayerCg);
				_cgSettingsMap[CGTemplateEnums.QuestionLower] = (_cgSettings.QuestionLower.Channel, _cgSettings.QuestionLower.TemplateName, _cgSettings.QuestionLower.Layer, _cgSettings.QuestionLower.LayerCg);
				_cgSettingsMap[CGTemplateEnums.Countdown] = (_cgSettings.CountDown.Channel, _cgSettings.CountDown.TemplateName, _cgSettings.CountDown.Layer, _cgSettings.CountDown.LayerCg);
				_cgSettingsMap[CGTemplateEnums.LeaderBoard] = (_cgSettings.LeaderBoard.Channel, _cgSettings.LeaderBoard.TemplateName, _cgSettings.LeaderBoard.Layer, _cgSettings.LeaderBoard.LayerCg);
				_cgSettingsMap[CGTemplateEnums.YTVote] = (_cgSettings.YTVote.Channel, _cgSettings.YTVote.TemplateName, _cgSettings.YTVote.Layer, _cgSettings.YTVote.LayerCg);
				_cgSettingsMap[CGTemplateEnums.QuestionVideo] = (_cgSettings.QuestionVideo.Channel, _cgSettings.QuestionVideo.TemplateName, _cgSettings.QuestionVideo.Layer, _cgSettings.QuestionVideo.LayerCg);
			}
			_casparCGWsService = casparCGWsService;


			//_connectionMnHost = _connectionMnHost ?? CreateAmcpConnection(_cgSettings.ServerIp, _cgSettings.ServerPort);


		}

		#region LightControl

		public void SetLightControlEnabled(bool isEnabled)
		{
			_midiLightingService.IsLightControlEnabled = isEnabled;
		}

		public async Task SendMIDInote(int Notenumber, int Velocity)
		{
			_midiLightingService.SendNoteOn(Notenumber, Velocity);		
		}
		public void ConnectMidiDevice()
		{
			_midiLightingService.Connect();
		}

		// ახალი მეთოდი UI-დან კავშირის გასაწყვეტად
		public void DisconnectMidiDevice()
		{
			_midiLightingService.Disconnect();
		}
		private async void OnMidiConnectionStatusChanged(object? sender, bool isConnected)
		{
			// გაუგზავნეთ შეტყობინება ყველა კლიენტს, განსაკუთრებით ოპერატორის UI-ს
			
			var operatorConnectionId = GetOperatorConnectionId();
			if (operatorConnectionId != null)
			{
				//await Clients.Client(operatorConnectionId).SendAsync("ReceiveMidiStatus", isConnected);


				try
				{
					await Clients.All.SendAsync("ReceiveMidiStatus", isConnected);
				}
				catch (Exception ex)
				{
					_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} exception in {MethodBase.GetCurrentMethod().Name} {ex.Message}");
					
				}
			}
		}
		#endregion

		#region CasparCG LeaderBoard
		// Update LeaderBoard in real-time
		public async Task CGSWUpdateLeaderBoard(bool isFinal = false)
		{
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now}  UpdateLeaderBoard isFinal '{isFinal}'");

			if (!_isLeaderBoardActive && !isFinal) return;

			var players = _gameService.ConnectedPlayers.Values
				.Where(p => p.ClientType =="Contestant")
				.OrderByDescending(p => p.Score)
				.Select(p => new { id = p.ConnectionId, name = p.NickName, score = p.Score })
				.ToList();

			var message = new
			{
				type = isFinal ? "show_final_results" : "update_LeaderBoard",
				players = players
			};

			await _casparCGWsService.SendDataToTemplateAsync("LeaderBoard", message);
		}

		public async Task CGSWStoreFinalResults()
		{
			await Task.Run(() =>
			{
				_finalScores = _gameService.ConnectedPlayers.Values
					.Select(p => new PlayerScore
					{
						PlayerId = p.ConnectionId,
						PlayerName = p.Name,
						Score = p.Score,
						Timestamp = DateTime.Now
					})
					.ToList();
			});

			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Final results stored for session.");
		}


		// Show final results
		public async Task CGSWShowFinalResults()
		{
			var players = _gameService.ConnectedPlayers.Values.Where(p => p.Score > 0 && p.ClientType == ClientTypes.Contestant.ToString())
				.Select(p => new { id = p.ConnectionId, name = p.Name, score = p.Score })
				.OrderByDescending(s=> s.score)
				.ToList();

			var message = new
			{
				type = "show_final_results",
				players = players
			};

			await _casparCGWsService.SendDataToTemplateAsync("LeaderBoard", message);
		}

		public async Task CGSWToggleLeaderBoard(bool isVisible)
		{
			_isLeaderBoardActive = isVisible;

			if (!isVisible)
			{
				var message = new { type = "clear" };
				await _casparCGWsService.SendDataToTemplateAsync("LeaderBoard", message);
			}
			else
			{
				await CGSWUpdateLeaderBoard();
			}
		}

		private async Task CGSWOnPlayerScoreChanged()
		{
			if (_isLeaderBoardActive)
			{
				await CGSWUpdateLeaderBoard();
			}
		}




		#endregion


		#region CasparCG

		public async Task CGWSClearChannelLayer(CGTemplateEnums templateType)
		{


			var data = new
			{
				type = "clear_content",
				Question = "",
				QuestionImage = "",
				Answers = ""
			};
						
			await _casparCGWsService.SendDataToTemplateAsync(templateType.ToString(), data);

			

		}

		public async Task CGClearChannel(CGTemplateEnums templateType)
		{
			//var channel = -1;


			var settings = _cgSettingsMap.GetValueOrDefault(templateType);
			if (settings.channel > 0)
			{
				await _caspar.ClearChannel(settings.channel);
			}

			//if (templateType == CGTemplateEnums.QuestionFull)
			//{
			//	channel = _cgSettings.QuestionFull.Channel;
			//}
			//else if (templateType == CGTemplateEnums.QuestionLower)
			//{
			//	channel = _cgSettings.QuestionLower.Channel;
			//}
			//else if (templateType == CGTemplateEnums.Countdown)
			//{
			//	channel = _cgSettings.CountDown.Channel;
			//}
			//else if (templateType == CGTemplateEnums.LeaderBoard)
			//{
			//	channel = _cgSettings.LeaderBoard.Channel;
			//}
			//else if (templateType == CGTemplateEnums.YTVote)
			//{
			//	channel = _cgSettings.YTVote.Channel;
			//}
			//else if (templateType == CGTemplateEnums.QuestionVideo)
			//{
			//	channel = _cgSettings.QuestionVideo.Channel;
			//}

			//await _caspar.ClearChannel(channel);
		}

		public async Task<OperationResult> CGLoadTemplate(CGTemplateEnums templateType)
		{
			var (channel, templateName, layer, _) = _cgSettingsMap.GetValueOrDefault(templateType);
			if (string.IsNullOrEmpty(templateName))
			{
				return new OperationResult(false);
			}
			return await CGEnsureTemplateLoadedAsync(templateName, channel, layer);
		}
		public async Task<OperationResult> CGLoadTemplate_(CGTemplateEnums templateType)
		{
			var res = new OperationResult(true);
			var templateName = string.Empty;
			var layer = -1;
			var channel = -1;
			var layerCg = -1;



			if (templateType == CGTemplateEnums.QuestionFull)
			{
				templateName = _cgSettings.QuestionFull.TemplateName;
				channel = _cgSettings.QuestionFull.Channel;
				layer = _cgSettings.QuestionFull.Layer;
				layerCg = _cgSettings.QuestionFull.LayerCg;
			}
			else if (templateType == CGTemplateEnums.QuestionLower)
			{
				templateName = _cgSettings.QuestionLower.TemplateName;
				channel = _cgSettings.QuestionLower.Channel;
				layer = _cgSettings.QuestionLower.Layer;
				layerCg = _cgSettings.QuestionLower.LayerCg;
			}
			else if (templateType == CGTemplateEnums.Countdown)
			{
				templateName = _cgSettings.CountDown.TemplateName;
				channel = _cgSettings.CountDown.Channel;
				layer = _cgSettings.CountDown.Layer;
				layerCg = _cgSettings.CountDown.LayerCg;
			}
			else if (templateType == CGTemplateEnums.LeaderBoard)
			{
				templateName = _cgSettings.LeaderBoard.TemplateName;
				channel = _cgSettings.LeaderBoard.Channel;
				layer = _cgSettings.LeaderBoard.Layer;
				layerCg = _cgSettings.LeaderBoard.LayerCg;
			}
			else if (templateType == CGTemplateEnums.YTVote)
			{
				templateName = _cgSettings.YTVote.TemplateName;
				channel = _cgSettings.YTVote.Channel;
				layer = _cgSettings.YTVote.Layer;
				layerCg = _cgSettings.YTVote.LayerCg;
			}




			//_ = CGEnsureTemplateLoadedAsync(templateName, channel, layer);
			return await CGEnsureTemplateLoadedAsync(templateName, channel, layer);

			


		}
		public async Task CGWSShowCorrectAnswer(string templateType, int correctAnswerIndex)
		{
			Console.WriteLine($"{DateTime.Now} Operator requested to show correct answer for template '{templateType}'. Correct answer index: {correctAnswerIndex}");



			// Prepare the message to send to the template via WebSocket
			var message = new
			{
				type = "show_answer",
				correctAnswerIndex = correctAnswerIndex
			};

			// Send the message to the specified template
			await CGWSUpdateInQuestionShowCorrectTemplateData(templateType, message);
		}
		public async Task CGWSUpdateInQuestionShowCorrectTemplateData(string templateType, object message)

		{
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Operator requested to update template fro Correct Answer '{templateType}' {message}");
						
			await _casparCGWsService.SendDataToTemplateAsync(templateType, message);
		}
		public async Task CGWSUpdateQuestionTemplateData_(string templateType, QuestionModel question)
		{
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Operator requested to update template '{templateType}' for Question {question}");

			var data = new
			{
				type = "show_question",
				Question = question.Question,
				QuestionImage = question.QuestionImage,
				Answers = question.Answers
			};

			// Use the new method that targets a specific template
			await _casparCGWsService.SendDataToTemplateAsync(templateType, data);
		}

		public async Task CGWSUpdateQuestionTemplateData(string templateType, QuestionModel question)
		{
			var data = new
			{
				type = "show_question",
				Question = question.Question,
				QuestionImage = question.QuestionImage,
				Answers = question.Answers
			};
			await _casparCGWsService.SendDataToTemplateAsync(templateType, data);
		}

		public async Task CGWSCountdown_(string templateType, int duration, CountdownStopMode action, long endTimestamp)
		{

			if (_currentGameMode == GameMode.Round1)
				return;

			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now}  Trigger Countdown Start in Template '{templateType}' for {action} sec");

			var message = new
			{
				type = action.ToString(),
				endTime = endTimestamp
			};

			
			await _casparCGWsService.SendDataToTemplateAsync(templateType, message);
		}

		public async Task CGWSCountdown(string templateType, int duration, CountdownStopMode action, long endTimestamp)
		{
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now}  Trigger Countdown Start in Template '{templateType}' for {action} sec");
			if (_currentGameMode == GameMode.Round1) return;
			var message = new { type = action.ToString(), endTime = endTimestamp };
			await _casparCGWsService.SendDataToTemplateAsync(templateType, message);
		}

		public async Task CGWSYTVote(string templateType, VoteResultsMessage message)
		{
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now}  YT graphic  sec");

;

			// Use the new method that targets a specific template
			await _casparCGWsService.SendDataToTemplateAsync(templateType, message);
		}


		public async Task<OperationResult> CGEnsureTemplateLoadedAsync(string templateName, int channel, int layer)
		{
			var res = new OperationResult(true);
			var key = $"{channel}-{layer}";
			if (!_isTemplateLoaded.ContainsKey(key))
			{
				res = await _caspar.LoadTemplate(templateName, channel, layer, 1, false, null);

			}
			return res;
		}

		public async Task CGPlayClip(int channel, int layer, string templateName)
		{
			templateName = _cgSettings.QuestionVideo.TemplateName + templateName;
			templateName = templateName.Replace("\\", "/").Replace(".mp4","");
			await _caspar.PlayClip(channel, layer, templateName);
		}

		public async Task CGClearPlayClip(CGTemplateEnums templateType)
		{
			var (channel, templateName, layer, _) = _cgSettingsMap.GetValueOrDefault(templateType);
			await _caspar.ClearChannelLayer(channel, layer);
		}

		public async Task CGSWSendScoreboardToCaspar(List<Player> players)
		{
			await CGSWOnPlayerScoreChanged();

		}
		#endregion




		public async Task ClientStartCountdown(int durationSeconds)
		{

			var endTimestamp = DateTimeOffset.UtcNow.AddSeconds(durationSeconds).ToUnixTimeMilliseconds();
			await Clients.All.SendAsync("ReceiveCountdown", endTimestamp);

			_isCountdownActiveOnCG = true;
		}
										  
		public async Task StartYTVoting()
		{
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} StartYTVoting: Voting Starting {"duration"} Secs.");
			if (_isYTVotingModeActive)
			{
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} StartYTVoting: Already Active, Return.");
				return;
			}

			_isYTVotingModeActive = true;
		
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} StartYTVoting: Getting for accessToken...");
			var accessToken = await _ytoauthTokenService.GetAccessTokenAsync();
			if (string.IsNullOrEmpty(accessToken))
			{
				_logger.LogError("StartYTVoting: Access Token not found. Cannot start.");
				return;
			}
			else {
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} StartVotingMode: Getting accessToken...DONE");
			}

			
		
		
			var videoId = _configuration["YTVotingSettings:Video_id"];
			if (string.IsNullOrEmpty(videoId))
			{
				_logger.LogWarning("StartVotingMode: Video_id not found in config.");
				return;
			}

			


			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} StartVotingMode: Getting LiveChatId...");
			var _liveChatId = await _youtubeChatService.GetLiveChatIdAsync(videoId, accessToken);
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} StartVotingMode: Getting LiveChatId...DONE " + _liveChatId);
		
		
		
			if (!string.IsNullOrEmpty(_liveChatId))
			{
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} StartVotingMode: Starting New Question...");
				await _ytAudienceVoteManager.StartNewQuestion(0, "შეკითხვა");
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} StartVotingMode: Starting New Question...DONE");
		
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} StartVotingMode: Sending Chat Message...");
				await _youtubeChatService.PostChatMessageAsync(_liveChatId, "ხმის მიცემა ჩართულია!", accessToken);
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} StartVotingMode: Sending Chat Message...DONE");
		
			}

			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} StartVotingMode: Method finished OK.");

		}

		
		public async Task StartVotingModeIndefinite()
		{
			if (_isYTVotingModeActive)
			{
				return;
			}

			_isYTVotingModeActive = true;
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} StartVotingModeIndefinite: Voting Starting for undefined Duration.");
			var accessToken = await _ytoauthTokenService.GetAccessTokenAsync();
			if (string.IsNullOrEmpty(accessToken))
			{
				_logger.LogError("StartVotingModeIndefinite: Access Token not found. Cannot start.");
				return;
			}
			else
			{
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} StartVotingModeIndefinite: Getting accessToken...DONE");
			}

			var videoId = _configuration["YTVotingSettings:Video_id"];
			if (string.IsNullOrEmpty(videoId))
			{
				_logger.LogWarning("YouTube Voting Video_id is not configured.");
				return;
			}
			var _liveChatId = await _youtubeChatService.GetLiveChatIdAsync(videoId, accessToken);

			if (!string.IsNullOrEmpty(_liveChatId))
			{
				await _ytAudienceVoteManager.StartNewQuestion(0, "შეკითხვა");
				await _youtubeChatService.PostChatMessageAsync(_liveChatId, "ხმის მიცემა ჩართულია!", accessToken);
			}
		}

		
		public async Task StopVotingMode()
		{
			if (!_isYTVotingModeActive)
			{
				return;
			}

			_isYTVotingModeActive = false;
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} ხმის მიცემა შეჩერდა.");

			var accessToken = await _ytoauthTokenService.GetAccessTokenAsync();
			var videoId = _configuration["YTVotingSettings:Video_id"];
			if (string.IsNullOrEmpty(videoId))
			{
				_logger.LogWarning("YouTube Voting Video_id is not configured.");
				return;
			}
			var _liveChatId = await _youtubeChatService.GetLiveChatIdAsync(videoId, accessToken);

			if (!string.IsNullOrEmpty(_liveChatId))
			{
				await _youtubeChatService.PostChatMessageAsync(_liveChatId, "ხმის მიცემა შეწყდა.", accessToken);
			}
		}

		
		public async Task KickUserFromVoting(string authorChannelId)
		{
			await _ytAudienceVoteManager.KickUserAsync(authorChannelId);
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} მომხმარებელი {authorChannelId} გაგდებულია.");

			var accessToken = await _ytoauthTokenService.GetAccessTokenAsync();
			var videoId = _configuration["YTVotingSettings:Video_id"];
			if (string.IsNullOrEmpty(videoId))
			{
				_logger.LogWarning("YouTube Voting Video_id is not configured.");
				return;
			}
			var _liveChatId = await _youtubeChatService.GetLiveChatIdAsync(videoId, accessToken);

			if (!string.IsNullOrEmpty(_liveChatId) && !string.IsNullOrEmpty(authorChannelId))
			{
				await _youtubeChatService.PostChatMessageAsync(_liveChatId, $"მომხმარებელი გაგდებულია თამაშიდან.", accessToken);
			}
		}

		public async Task ReceiveAudienceDat(string accessToken)
		{
			//TODO
			return;
		}

		public async Task StartYTDataCollectingAsync()
		{
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} GameHub:StartYTDataCollectingAsync:  Going to star Collector Service.");
			await _ytdataCollectorService.StartCollectingAsync();
		}
		public async Task SaveYTOAuthTokens(string accessToken, string refreshToken)
		{

			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} YouTube OAuth Tokens Saved in GameHub-ში.  accessToken {accessToken}, refreshToken {refreshToken}" );
			// ვინახავთ ტოკენებს OAuthTokenService-ში
			await _ytoauthTokenService.SaveTokensAsync(accessToken, refreshToken);

			/////	დროებით აქ აღარ დავიწყებ
				var startedDataColletor = await _ytdataCollectorService.StartCollectingAsync();
			/////	
			/////	
			/////	
			/////	var operatorConnectionId = GetOperatorConnectionId();
			/////	if (operatorConnectionId != null)
			/////	{
			/////		await Clients.Client(operatorConnectionId).SendAsync("ReceiveMessage","YouTube",startedDataColletor.Message);
			/////	}

		}

		
		public async Task<List<QuestionModel>> LoadQuestionsFromFile(string fileContent)
		{
			try
			{
				// Deserialize the file content into our Question list on a background thread
				_questions = await Task.Run(() => System.Text.Json.JsonSerializer.Deserialize<List<QuestionModel>>(fileContent));
				int questionCount = _questions?.Count ?? 0;
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Successfully loaded {questionCount} questions from a file");
				return _questions ?? new List<QuestionModel>();
			}
			catch (System.Text.Json.JsonException ex)
			{
				_logger.LogError("Failed to deserialize JSON content: {Message}", ex.Message);
				throw;
			}
		}


		private string? GetOperatorConnectionId()
		{

			


			if (string.IsNullOrEmpty(_operatorConnectionId))
			{
				// This log is a good way to debug if the operator hasn't connected yet.
				_logger.LogWarning("Attempted to get operator ConnectionId, but none was found.");
			}
			return _operatorConnectionId;
		}

		private string? GetHostConnectionId()
		{




			if (string.IsNullOrEmpty(_hostConnectionId))
			{
				// This log is a good way to debug if the operator hasn't connected yet.
				_logger.LogWarning("Attempted to get HOST ConnectionId, but none was found.");
			}
			return _hostConnectionId;
		}

		private string? GetYTVoteMangerConnectionId()
		{




			if (string.IsNullOrEmpty(_ytVoterManagerConnectionId))
			{
				// This log is a good way to debug if the operator hasn't connected yet.
				_logger.LogWarning("Attempted to get _ytVoterManagerConnectionId ConnectionId, but none was found.");
			}
			return _ytVoterManagerConnectionId;
		}

		public List<Player> GetRegisteredPlayers()
		{

			// Simply return the list of players from the concurrent dictionary
			return _gameService.ConnectedPlayers.Values.ToList();
		}

		public override async Task OnConnectedAsync()
		{
			

			var httpContext = Context.GetHttpContext();
			var clientIp = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";

			

			// Registration logic
			var clientName = httpContext?.Request?.Query["name"].ToString(); // Assume name is passed as query parameter
			//var isRegisteredClient = _registeredClients.FirstOrDefault(c => clientIp?.Contains(c.ip)  && c.clientName == clientName);
			//// Replace this line:
			//var isRegisteredClient = _registeredClients.FirstOrDefault(c => clientIp?.Contains(c.ip) && c.clientName == clientName);

			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Client connected. ConnectionId: {Context.ConnectionId}, IP: {clientIp}, Name: {clientName}");

			// With this corrected version:
			var isRegisteredClient = _registeredClients.FirstOrDefault(
				c => c.ip.Any(ip => !string.IsNullOrEmpty(ip) && clientIp?.Contains(ip) == true) && c.clientName.ToLower() == clientName.ToLower()
			);
			if (isRegisteredClient != null)
			{

				var newPlayer = _gameService.AddNewPlayer(Context.ConnectionId, clientIp, clientName ?? string.Empty,
					isRegisteredClient.nickName,
					isRegisteredClient.clientType, isRegisteredClient.clientType == ClientTypes.Contestant.ToString() ? true : false);
				

                // Check if a player with this IP and name already exists in our dictionary of connected players
                var existingPlayer = _gameService.ConnectedPlayers.Values
                    .FirstOrDefault(p => p.Name.Equals(clientName, StringComparison.OrdinalIgnoreCase) && p.Ip == clientIp);

                if (existingPlayer != null)
                {
                    // Player with this IP/name combo already exists. This is a re-connection.
                    // We need to remove the old entry and add the new one with the updated ConnectionId.
                    if (_gameService.ConnectedPlayers.TryRemove(existingPlayer.ConnectionId, out _))
                    {
                        _logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Client '{existingPlayer.Name}' from {existingPlayer.Ip} reconnected. Removed old entry with ConnectionId: {existingPlayer.ConnectionId}.");
                    }
                    // Note: The new player will be added in the next step.
                }

                // If the connected client is the operator, save their ConnectionId
                if (newPlayer.ClientType == "Operator")
				{
					_operatorConnectionId = Context.ConnectionId;
				}
				if (newPlayer.ClientType == "YTVoteManager")
				{
					_ytVoterManagerConnectionId = Context.ConnectionId;
				}
				if (newPlayer.ClientType == "FBVoteManager")
				{
					_fbVoterManagerConnectionId = Context.ConnectionId;
				}
				if (newPlayer.ClientType == "Host")
				{
					_hostConnectionId = Context.ConnectionId;
				}

				_gameService.ConnectedPlayers.AddOrUpdate(Context.ConnectionId, newPlayer, (key, existingPlayer) => newPlayer);

				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Client {Context.ConnectionId} '{isRegisteredClient.clientName}' from {clientIp} {isRegisteredClient.clientType} is REGISTERED. Total registered players: {_gameService.ConnectedPlayers.Count}");

				await Clients.Caller.SendAsync("UpdateRegistrationStatus", true, newPlayer.Name, Context.ConnectionId, isRegisteredClient.clientType);

				//await Clients.Caller.SendAsync("ReceiveRegistrationStatus", "Registered");
				await Clients.All.SendAsync("UpdatePlayerList", _gameService.ConnectedPlayers.Values.ToList());

				if (isRegisteredClient.clientType == "Operator")
				{
					await Clients.Caller.SendAsync("ReceiveQuestionList", _questions);
				}


				await Clients.Client(Context.ConnectionId).SendAsync("ReceiveMidiStatus", _midiLightingService.IsConnected);


				// TODO: Notify UI about new player
			}
			else
			{
				_logger.LogWarning($"Client from {clientIp} with name '{clientName}' is not registered.");
				await Clients.Caller.SendAsync("ReceiveRegistrationStatus", "NotRegistered");
				await Clients.Caller.SendAsync("ReceiveMessage", "You are not a registered player and cannot participate.");
			}

			await base.OnConnectedAsync();
		}

		public override async Task OnDisconnectedAsync(System.Exception? exception)
		{

			if (_gameService.ConnectedPlayers.TryRemove(Context.ConnectionId, out var player))
			{
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Client '{player.Name}' disconnected. Total registered players: {_gameService.ConnectedPlayers.Count}");
				await Clients.All.SendAsync("UpdatePlayerList", _gameService.ConnectedPlayers.Values.ToList());

				
			}
			else
			{
				_logger.LogWarning($"Unknown client disconnected with ConnectionId: {Context.ConnectionId}");
			}
			await base.OnDisconnectedAsync(exception);
		}

		public async Task SendMessageToClient(string connectionId, string message)
		{
			// შეამოწმეთ, არსებობს თუ არა კლიენტი ამ Connection ID-ით
			if (_gameService.ConnectedPlayers.ContainsKey(connectionId))
			{
				var sender = _gameService.ConnectedPlayers[Context.ConnectionId];
				await Clients.Client(connectionId).SendAsync("ReceiveCustomMessage", sender.Name, message);
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Message from '{sender.Name}' sent to client '{connectionId}': {message}");
			}
			else
			{
				_logger.LogWarning($"Failed to send message: Client with Connection ID '{connectionId}' not found.");
			}
		}



		public async Task SendQuestion_(QuestionModel question, int durationSeconds, GameMode mode, bool disableInput, List<Player>? clients)
		{

			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Sending question '{question.Question}' with a {durationSeconds}s countdown.");

			_activeQuestion = question; // Set the active question here
			_answersReceivedCount = 0; // Reset the counter for each new question
			//_currentCountdownMode = mode; // Store the selected mode
			_currentGameMode = mode;
			_currentDisableInput = disableInput; // Store the input disable state
			//_isRapidFireActive = false; 

			_midiLightingService.SendNoteOn(
				_midiSettings.CountdownNote,
				_midiSettings.CountdownVelocity
			);



			var endTimestamp = DateTimeOffset.UtcNow.AddSeconds(durationSeconds).ToUnixTimeMilliseconds();
			if (clients == null || clients.Count == 0)
			{
				// თუ სია არ არის გადმოცემული ან ცარიელია, გაუგზავნეთ ყველას
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} No specific clients provided. Sending to all connected clients.");

				_activePlayerIds = _gameService.ConnectedPlayers.Where(p => p.Value.ClientType == "Contestant").Select(p => p.Key).ToList();
				await Clients.All.SendAsync("ReceiveQuestion", question, disableInput);
				await Clients.All.SendAsync("ReceiveCountdown", endTimestamp);
			}
			else
			{
				// გაუგზავნეთ კითხვა მხოლოდ კონკრეტულ კლიენტებს
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Sending question to {clients.Count} specified clients.");
				_activePlayerIds = clients.Select(c=> c.ConnectionId).ToList();

				await Clients.Clients(_activePlayerIds).SendAsync("ReceiveQuestion", question, disableInput);
				await Clients.Clients(_activePlayerIds).SendAsync("ReceiveCountdown", endTimestamp);
			}

			
			
			
			await Clients.Clients(GetOperatorConnectionId()).SendAsync("ReceiveCountdown", endTimestamp);
			await CGWSCountdown(CGTemplateEnums.Countdown.ToString(), durationSeconds, CountdownStopMode.Start, endTimestamp);
			


			await CGWSUpdateQuestionTemplateData(CGTemplateEnums.QuestionFull.ToString(), question);
			await CGWSUpdateQuestionTemplateData(CGTemplateEnums.QuestionLower.ToString(), question);
			if (!String.IsNullOrEmpty(question.QuestionVideo))
			{
				await CGPlayClip(_cgSettings.QuestionVideo.Channel, _cgSettings.QuestionVideo.Layer, question.QuestionVideo);
			}
		}


		public async Task SendQuestion(QuestionModel question, int durationSeconds, GameMode mode, bool disableInput, List<Player>? clients)
		{
			_logger.LogInformation($"Sending question '{question.Question}' with a {durationSeconds}s countdown.");

			_activeQuestion = question;
			_answersReceivedCount = 0;
			_currentGameMode = mode;

			_midiLightingService.SendNoteOn(_midiSettings.CountdownNote, _midiSettings.CountdownVelocity);

			var endTimestamp = DateTimeOffset.UtcNow.AddSeconds(durationSeconds).ToUnixTimeMilliseconds();

			_activePlayerIds = clients?.Select(c => c.ConnectionId).ToList() ?? _gameService.ConnectedPlayers.Where(p => p.Value.ClientType == "Contestant").Select(p => p.Key).ToList();

			var clientGroup = clients != null && clients.Count > 0
				? Clients.Clients(_activePlayerIds)
				: Clients.All;

			await clientGroup.SendAsync("ReceiveQuestion", question, disableInput);
			await clientGroup.SendAsync("ReceiveCountdown", endTimestamp);
			await Clients.Client(GetOperatorConnectionId()).SendAsync("ReceiveCountdown", endTimestamp);

			await CGWSCountdown(CGTemplateEnums.Countdown.ToString(), durationSeconds, CountdownStopMode.Start, endTimestamp);
			await CGWSUpdateQuestionTemplateData(CGTemplateEnums.QuestionFull.ToString(), question);
			await CGWSUpdateQuestionTemplateData(CGTemplateEnums.QuestionLower.ToString(), question);

			if (!string.IsNullOrEmpty(question.QuestionVideo))
			{
				await CGPlayClip(_cgSettings.QuestionVideo.Channel, _cgSettings.QuestionVideo.Layer, question.QuestionVideo);
				
			}
		}
		public async Task UpdateScoresFromUIToMEM(List<Player> players)
		{

			var incomingPlayersLookup = players.ToDictionary(p => p.ConnectionId, p => p);

			bool scoresUpdated = false;

			// გადავიაროთ ყველა დაკავშირებულ მოთამაშეზე სერვერზე
			foreach (var connectedPlayer in _gameService.ConnectedPlayers.Values)
			{
				// შევამოწმოთ, არის თუ არა ეს მოთამაშე შემოსულ სიაში მისი ConnectionId-ით
				if (incomingPlayersLookup.TryGetValue(connectedPlayer.ConnectionId, out var incomingPlayer))
				{
					// შევამოწმოთ, ქულები განსხვავდება თუ არა
					if (connectedPlayer.Score != incomingPlayer.Score)
					{
						// თუ ქულა განსხვავებულია, განვაახლოთ
						connectedPlayer.Score = incomingPlayer.Score;
						scoresUpdated = true; // დავაყენოთ დროშა, რომ ცვლილება მოხდა

						_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Updated score for player '{connectedPlayer.Name}' from {connectedPlayer.Score} to {incomingPlayer.Score}");
					}
				}
			}

			if (scoresUpdated)
			{
				//await Clients.All.SendAsync("UpdatePlayerList", _connectedPlayers.Values.ToList());
				await Task.CompletedTask;

			}


		}

		public async Task SubmitAnswer(string selectedAnswer)
		{
			var playerConnectionId = Context.ConnectionId;

			// Check if the player is active for the current question
			if (!_activePlayerIds.Contains(playerConnectionId))
			{
				_gameService.ConnectedPlayers.TryGetValue(playerConnectionId, out var notActivePlayer);
				_logger.LogWarning($"Client '{playerConnectionId}' {notActivePlayer?.Name} is not an active player for this question. Skipping.");
				return;
			}

			if (_gameService.ConnectedPlayers.TryGetValue(playerConnectionId, out var player))
			{
				if (_activeQuestion != null)
				{
					// Update score regardless of the mode
					if (selectedAnswer == _activeQuestion.CorrectAnswer)
					{
						//player.AddScore(1);
						_gameService.AddPoints(player,1);
						_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Player '{player.Name}' submitted a CORRECT answer. Score: {player.Score}");
					}
					else
					{
						_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Player '{player.Name}' submitted an INCORRECT answer.");
					}

					// This is the core logic for the new RapidFire UI control
					//if (_isRapidFireActive)
					if (_currentGameMode == GameMode.RapidMode)
					{
						_logger.LogDebug($"{_currentGameMode.ToString()} is active. Notifying the operator to send the next question.");

						// Notify the operator UI that a player has answered.
						await Clients.Client(GetOperatorConnectionId()).SendAsync("PlayerAnsweredInRapidFire");
						await Clients.All.SendAsync("UpdatePlayerList", _gameService.ConnectedPlayers.Values.ToList());
						if (_isLeaderBoardActive)
						{
							var players = _gameService.ConnectedPlayers.Values.OrderByDescending(p => p.Score).ToList();
							await CGSWSendScoreboardToCaspar(players);
						}

						// Exit the method here to prevent any other logic from running in RapidFire mode
						return;
					}
					else
					{
						//if (_currentCountdownMode == CountdownMode.FirstAnswer)
						if (_currentGameMode == GameMode.FirstAnswer)
						{
							_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} active player have submitted an answer. Stopping countdown.");
							await Clients.All.SendAsync("StopCountdown", CountdownStopMode.Pause.ToString()); // Send a signal to stop the countdown

						}
					}

					// This is the old logic for non-RapidFire modes
					if (_currentGameMode == GameMode.AllPlayersAnswered)
					{
						_answersReceivedCount++;
						if (_answersReceivedCount >= _activePlayerIds.Count)
						{
							_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} All active players have submitted an answer. Stopping countdown.");
							await Clients.All.SendAsync("StopCountdown", CountdownStopMode.TimeUp.ToString()); // Send a signal to stop the countdown
						}
					}

					await Clients.Caller.SendAsync("ReceiveAnswerStatus", selectedAnswer == _activeQuestion.CorrectAnswer);
					await Clients.All.SendAsync("UpdatePlayerList", _gameService.ConnectedPlayers.Values.ToList());
					if (_isLeaderBoardActive)
					{
						var players = _gameService.ConnectedPlayers.Values.OrderByDescending(p => p.Score).ToList();
						await CGSWSendScoreboardToCaspar(players);
					}
				}
				else
				{
					_logger.LogWarning($"Player '{player.Name}' submitted an answer, but no active question was found.");
				}
			}
		}

		


		public async Task EndRound(RoundEndAction action = RoundEndAction.Reset)
		{
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Round has ended. Processing results.");

			// Clear the active question and player list for the next round.
			_isRapidFireActive = false; // Reset the flag
			_currentGameMode = GameMode.None;
			_activeQuestion = null;
			_activePlayerIds.Clear();
			_answersReceivedCount = 0;
			_rapidFireCurrentQuestionIndex = -1;

			// Send a signal to all clients that the round has ended.
			// We can also include information about the winner or scores here.
			await Clients.All.SendAsync("RoundEnded", action);
		}


		public async Task SendRapidFireQuestionFromUI(QuestionModel nextQuestion, bool disableInput)
		{
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Rapid Fire: Sending question '{nextQuestion.Question}'");

			_activeQuestion = nextQuestion;
			_currentDisableInput = disableInput;

			// Send the new question to the active players
			await Clients.Clients(_activePlayerIds).SendAsync("ReceiveQuestion", nextQuestion, _currentDisableInput);
			await CGWSUpdateQuestionTemplateData(CGTemplateEnums.QuestionFull.ToString(), nextQuestion);
			await CGWSUpdateQuestionTemplateData(CGTemplateEnums.QuestionLower.ToString(), nextQuestion);
		}
		public async Task StartRapidFire(List<Player> clients, int durationSeconds, bool disableInput)
		{
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Starting Rapid Fire mode.");
			//_isRapidFireActive = true; // Set the flag to true
			_currentGameMode = GameMode.RapidMode;
			_currentDisableInput = disableInput;



			_activePlayerIds = clients.Select(c=> c.ConnectionId).ToList();// ?? _connectedPlayers.Where(p => p.Value.ClientType == "Contestant").Select(p => p.Key).ToList();

			// Set up the countdown for the entire Rapid Fire session
			var endTimestamp = DateTimeOffset.UtcNow.AddSeconds(durationSeconds).ToUnixTimeMilliseconds();
			await Clients.Clients(_activePlayerIds).SendAsync("ReceiveCountdown", endTimestamp);

			await Clients.Clients(GetOperatorConnectionId()).SendAsync("ReceiveCountdown", endTimestamp);
			

			await CGWSCountdown(CGTemplateEnums.Countdown.ToString(), durationSeconds, CountdownStopMode.Start, endTimestamp);

		}

		public async Task Uuups(LastAction _lastAction )
		{
			var activePlayerId = _activePlayerIds.FirstOrDefault();
			if (activePlayerId != null && _gameService.ConnectedPlayers.TryGetValue(activePlayerId, out var player))
			{
				if (_lastAction == LastAction.Correct)
					player.AddScore(-1);
				else
					player.AddScore(1);
				
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Corrected Score for '{player.Name}'. Score: {player.Score}");


				await Clients.All.SendAsync("UpdatePlayerList", _gameService.ConnectedPlayers.Values.ToList());



				// NEW: Check if the scoreboard is currently active and update it
				if (_isLeaderBoardActive)
				{
					var players = _gameService.ConnectedPlayers.Values.OrderByDescending(p => p.Score).ToList();
					await CGSWSendScoreboardToCaspar(players);
				}

			}
		}

		public async Task OperatorConfirmAnswer(bool isCorrect, string? activePlayerID = null)
		{
			if (_activeQuestion == null)
			{
				_logger.LogWarning("Operator attempted to confirm an answer, but no active question was found.");
				return;
			}

			if (_currentGameMode == GameMode.RapidMode || _currentGameMode == GameMode.Round1)
			{
				// Add score to the active player if the answer was correct
				if (isCorrect)
				{
					 
					var activePlayerId = activePlayerID ?? _activePlayerIds.FirstOrDefault();
					if (activePlayerId != null && _gameService.ConnectedPlayers.TryGetValue(activePlayerId, out var player))
					{
						player.AddScore(_activeQuestion.ScorePrice);
						_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} RapidFire No Input: Operator confirmed a correct answer for player '{player.Name}'. Score: {player.Score}");
						

					}
				}
				else
				{
					_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} RapidFire No Input: Operator confirmed an incorrect answer.");

				}


				if (_currentGameMode == GameMode.Round1 && !string.IsNullOrEmpty(_activeQuestion.QuestionVideo) && !string.IsNullOrEmpty(_activeQuestion.CorrectAnswerVideo))
				{
					await CGPlayClip(_cgSettings.QuestionVideo.Channel, _cgSettings.QuestionVideo.Layer, _activeQuestion.CorrectAnswerVideo);
				}

				// IMPORTANT: Update the player list for all clients to show the new score
				await Clients.All.SendAsync("UpdatePlayerList", _gameService.ConnectedPlayers.Values.ToList());
				

				
				// NEW: Check if the scoreboard is currently active and update it
				if (_isLeaderBoardActive)
				{
					var players = _gameService.ConnectedPlayers.Values.OrderByDescending(p => p.Score).ToList();
					await CGSWSendScoreboardToCaspar(players);
				}


				var operatorConnectionId = GetOperatorConnectionId();
				if (operatorConnectionId != null)
				{
					await Clients.Client(operatorConnectionId).SendAsync("OperatorConfirmedAnswer");
				}

				
			}
		}


		public Task SendConnectionId(ConnectionIdMessage message)
		{
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Received Connection ID from service {message.ServiceName}, {message.ConnectionId}");

			// Sends the connection ID to all clients except the sender.
			return Clients.Others.SendAsync("ReceiveConnectionId", message);
		}


		public async Task PassMessageToYTVoteManager(MessageToYTManager msg)
		{
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} GameHub: PassMessageToYTVoteManager");
			msg.SenderConnectionId = Context.ConnectionId;
			var destClientId = GetYTVoteMangerConnectionId();
			await Clients.Clients(destClientId).SendAsync("ReceiveMessageAsync", msg);
		}

		/// <summary>
		/// This method is called by the operator to start a voting process.
		/// </summary>
		/// <param name="message">The voting request message.</param>
		public async Task StartVoting(VoteRequestMessage message)
		{
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} GameHub: Received a voting request from an operator: IsVotingActive = {message.IsVotingActive}");
			var destClientId = GetYTVoteMangerConnectionId();
			await Clients.Clients(destClientId).SendAsync("ReceiveVoteRequest", message);
		}

		/// <summary>
		/// This method is called by the operator to stop a voting process.
		/// </summary>
		/// <param name="message">The voting request message.</param>
		public async Task StopVoting(VoteRequestMessage message)
		{
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} GameHub: Received a voting request from an operator: IsVotingActive = {message.IsVotingActive}");
			var destClientId = GetYTVoteMangerConnectionId();
			await Clients.Clients(destClientId).SendAsync("ReceiveVoteRequest", message);

		}
		public Task Ping()
		{

			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Received Ping from client: {Context.ConnectionId}");
			return Clients.Client(Context.ConnectionId).SendAsync("Pong");

		}


		public async Task ReceiveVoteResults(VoteResultsMessage message)
		{
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} GameHub: Received vote results from YouTubeChatService");
			// This is where we would trigger the CasparCG template.
			// For now, we are just logging the received data.
			var chatMessageBuilder = new StringBuilder();
			

			chatMessageBuilder.AppendLine($"🗳️ სულ დარეგისტრირდა {message.Results.Sum(x => x.Count)} პასუხი:");
			
			foreach (var result in message.Results)
			{
				chatMessageBuilder.AppendLine($"   ✔️ {result.Answer}: {result.Count} ({result.Percentage:F2}%)");
			}
			
			chatMessageBuilder.AppendLine("");
			chatMessageBuilder.AppendLine("🙏 გმადლობთ მონაწილეობისთვის!");


			var operatorConnectionId = GetOperatorConnectionId();
			if (operatorConnectionId != null)
			{
				await Clients.Client(operatorConnectionId).SendAsync("VoteResultsMessage", message);
			}

			await CGWSYTVote(CGTemplateEnums.YTVote.ToString(), message);
			
		}
	}
}
