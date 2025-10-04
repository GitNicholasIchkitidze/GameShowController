using CasparCg.AmcpClient.Commands.Query.Common;
using GameController.Server.Services;
using GameController.Server.VotingManagers;
using GameController.Server.VotingServices;
using GameController.Shared.Enums;
using GameController.Shared.Models;
using GameController.Shared.Models.Connection;
using GameController.Shared.Models.YouTube;
using GameController.UI.Model;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;

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
        //private static CountDownMode _currentCountDownMode;
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

        private static bool _isCountDownActiveOnCG = false; // To track if the CG countdown is running


        private readonly ConcurrentDictionary<string, bool> _isTemplateLoaded = new ConcurrentDictionary<string, bool>();
        private readonly ICasparService _caspar;
        private readonly CasparCGWsService _casparCGWsService;
        private readonly ICasparManager _casparManager;




		private readonly IYTAudienceVoteManager _ytAudienceVoteManager;
        private readonly IYouTubeDataCollectorService _ytdataCollectorService;
        private readonly IYouTubeChatService _youtubeChatService; // 

        public static string? YTaccessToken;
        public static string? YTrefreshToken;
        private readonly IYTOAuthTokenService _ytoauthTokenService;
        public static bool _isYTVotingModeActive = false; // ახალი
														  //private static ConcurrentDictionary<string, bool> _activeYTAudienceIds = new ConcurrentDictionary<string, bool>();


		//private static Dictionary<CGTemplateEnums, (string serverIP, int channel, string templateName, int layer, int layerCg)> _cgSettingsMap = new();
		//private static Dictionary<CGTemplateEnums, templateSettingModel> _cgSettingsMap = new();



		public GameHub(IMidiLightingService midiLightingService,
            IOptions<MidiSettingsModels> midiSettings,
            ILogger<GameHub> logger,
            IConfiguration configuration,
            IQuestionService questionService,
            ICasparService caspar,
            IGameService gameService,
            CasparCGWsService casparCGWsService,
			ICasparManager casparManager,

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
            _casparManager = casparManager;
            _casparCGWsService = casparCGWsService;
			_midiLightingService = midiLightingService;
            _midiSettings = midiSettings.Value;
            _gameService = gameService;

            _ytAudienceVoteManager = audienceVoteManager;
            _ytdataCollectorService = ytdataCollectorService;
            _ytoauthTokenService = ytoauthTokenService;
            _youtubeChatService = youtubeChatService;

            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} >>> GameHub instance created <<<");


            // გამოიწერეთ event-ი და გამოიძახეთ მეთოდი სტატუსის ცვლილებისას
            _midiLightingService.ConnectionStatusChanged += OnMidiConnectionStatusChanged;


            //if (_questions == null)
            //{
            //	_questions = questionService.LoadQuestionsAsync().Result;
            //}


            _cgSettings = _configuration.GetSection("CG").Get<CasparCGSettings>();


            if (_cgSettings != null)
            {
                ////_cgSettingsMap[CGTemplateEnums.QuestionFull] = (_cgSettings.QuestionFull.Channel, _cgSettings.QuestionFull.TemplateName, _cgSettings.QuestionFull.Layer, _cgSettings.QuestionFull.LayerCg);
                ////_cgSettingsMap[CGTemplateEnums.QuestionLower] = (_cgSettings.QuestionLower.Channel, _cgSettings.QuestionLower.TemplateName, _cgSettings.QuestionLower.Layer, _cgSettings.QuestionLower.LayerCg);
                ////_cgSettingsMap[CGTemplateEnums.CountDown] = (_cgSettings.CountDown.Channel, _cgSettings.CountDown.TemplateName, _cgSettings.CountDown.Layer, _cgSettings.CountDown.LayerCg);
                ////_cgSettingsMap[CGTemplateEnums.LeaderBoard] = (_cgSettings.LeaderBoard.Channel, _cgSettings.LeaderBoard.TemplateName, _cgSettings.LeaderBoard.Layer, _cgSettings.LeaderBoard.LayerCg);
                ////_cgSettingsMap[CGTemplateEnums.YTVote] = (_cgSettings.YTVote.Channel, _cgSettings.YTVote.TemplateName, _cgSettings.YTVote.Layer, _cgSettings.YTVote.LayerCg);
                ////_cgSettingsMap[CGTemplateEnums.QuestionVideo] = (_cgSettings.QuestionVideo.Channel, _cgSettings.QuestionVideo.TemplateName, _cgSettings.QuestionVideo.Layer, _cgSettings.QuestionVideo.LayerCg);
            }
            


            


        }

        #region LightControl

        public void SetLightControlEnabled(bool isEnabled)
        {
            _midiLightingService.IsLightControlEnabled = isEnabled;
        }

        public async Task SendMIDInote(int Notenumber, int Velocity)
        {
			//_midiLightingService.SendNoteOn(Notenumber, Velocity);
			await Task.CompletedTask;
		}
        public void ConnectMidiDevice()
        {
            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} GameHub: Switching MIDI ON");
            //_midiLightingService.Connect();
        }

        // ახალი მეთოდი UI-დან კავშირის გასაწყვეტად
        public void DisconnectMidiDevice()
        {
            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} GameHub: Switching MIDI OFF");
            //_midiLightingService.Disconnect();
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
                    _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} exception in {MethodBase.GetCurrentMethod().Name} {ex.Message}");

                }
            }
        }
		#endregion

		#region CasparCG LeaderBoard
		// Update LeaderBoard in real-time
		public async Task CGSWUpdateLeaderBoard(bool isFinal = false)
        {
			
				await _casparManager.CGSWUpdateLeaderBoardAsync(_isLeaderBoardActive, isFinal);

		}

		

		public async Task CGSWStoreFinalResults()
        {
			await _casparManager.CGSWStoreFinalResultsAsync();
		}

		
		public async Task CGSWShowFinalResults()
        {
			await _casparManager.CGSWShowFinalResultsAsync();
		}

		
		public async Task CGSWToggleLeaderBoard(bool isVisible)
        {
			_isLeaderBoardActive = isVisible;
            await _casparManager.CGSWToggleLeaderBoardAsync(_isLeaderBoardActive, isVisible);
		}
        	

        private async Task CGSWOnPlayerScoreChanged()
        {
           await _casparManager.CGSWUpdateLeaderBoardAsync(_isLeaderBoardActive);

        }

		



		#endregion


		#region CasparCG

		public async Task CGWSClearChannelLayer(CGTemplateEnums templateType)
        {
			await _casparManager.CGWSClearChannelLayerAsync(templateType);

		}
		

		public async Task CGClearChannel(CGTemplateEnums templateType)
        { 
            await _casparManager.ClearChannelAsync(templateType);
		}


		public async Task<OperationResult> UpdatecgSettingsMap(CGTemplateEnums templateType,Dictionary<CGTemplateEnums, templateSettingModel> cgSettingsMap)
        {
			//_cgSettingsMap = cgSettingsMap;
			var result = await _casparManager.UpdatecgSettingsMap(templateType, cgSettingsMap);

			return new OperationResult(result.Result);
			//var result = await _casparManager.LoadTemplateAsync(templateType);
			//return result;
		}

        public async Task<OperationResult> CGSWToggleTitle(TitleDataModel title, bool show)
        {
            var result = await _casparManager.ToggleTitle(title, show);
            if (!result.Result)
                _logger.LogError($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Failed to load template: {result.Message}");
            return result;

        }

        public async Task<OperationResult> CGLoadTemplate(CGTemplateEnums templateType)
		{
			var result = await _casparManager.LoadTemplateAsync(templateType);
			if (!result.Result)
				_logger.LogError($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Failed to load template: {result.Message}");
			return result;
		}
		////public async Task<OperationResult> CGLoadTemplateWithParams(CGTemplateEnums templateType,
		////	Dictionary<CGTemplateEnums, (string serverIP, int channel, string templateName, int layer, int layerCg)> _cgSettingsMap
        ////
		////	)
		////{
		////	var result = await _casparManager.LoadTemplateAsync(templateType, _cgSettingsMap);
		////	if (!result.Result)
		////		_logger.LogError($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Failed to load template: {result.Message}");
        ////    return result;
		////}

		
		public async Task CGWSShowCorrectAnswer(string templateType, int correctAnswerIndex)
		{
			var result = await _casparManager.ShowCorrectAnswerAsync(templateType, correctAnswerIndex);
			if (!result.Result)
				_logger.LogError($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Failed to load template: {result.Message}");

			// ლოგინგი სერვისშია
		}

		public async Task CGWSUpdateInQuestionShowCorrectTemplateData(string templateType, object message)
        {           

           await _casparManager.CGWSUpdateInQuestionShowCorrectTemplateDataAsync(templateType, message);
        }
				
		public async Task CGWSUpdateQuestionTemplateData(string templateType, QuestionModel question)
		{
			var result = await _casparManager.UpdateQuestionTemplateAsync(templateType, question);
			if (!result.Result)
				_logger.LogError($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Failed to update question template: {result.Message}");
		}
        		
		public async Task CGWSCountDown(string templateType, int duration, CountDownStopMode action, long endTimestamp)
		{
			await _casparManager.StartCountDownAsync(templateType, duration, action, endTimestamp);
		}

		public async Task CGWSYTVote(string templateType, VoteResultsMessage message)
        {
			await _casparManager.CGWSYTVoteAsync(templateType, message);
		}
		        
		public async Task<OperationResult> CGEnsureTemplateLoadedAsync(string templateName, int channel, int layer)
		{
			var res = new OperationResult(true);
			var key = $"{channel}-{layer}";
			const int maxRetries = 3;

			if (!_isTemplateLoaded.ContainsKey(key))
			{
				// დამატებული რეტრაი ლოგიკა
				int retryCount = 0;
				

				while (retryCount < maxRetries)
				{
					res = await _caspar.LoadTemplate(templateName, channel, layer, 1, false, null);

					if (res.Result)
					{
						_isTemplateLoaded[key] = true;
						_logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} EnsureTemplateLoaded: {res.Message}");
						break;
					}
					else
					{
						retryCount++;
						_logger.LogWarning($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} EnsureTemplateLoaded FAILED (attempt {retryCount}): {res.Message}");

						if (retryCount < maxRetries)
						{
							await Task.Delay(1000); // დაელოდეთ 1 წამს სანამ თავიდან ცდით
						}
					}
				}
			}

			if (!res.Result)
			{
				_logger.LogError($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} EnsureTemplateLoaded: FINAL FAILURE after {maxRetries} attempts: {res.Message}");
			}

			return res;
		}


		
		public async Task CGPlayClip(int channel, int layer, string templateName)
		{
			await _casparManager.PlayClipAsync(channel, layer, templateName);
		}
        		
		public async Task CGClearPlayClip(CGTemplateEnums templateType)
		{			
			await _casparManager.ClearPlayClipAsync(templateType);
		}

		public async Task CGSWSendScoreboardToCaspar(List<Player> players)
        {
            await CGSWOnPlayerScoreChanged();

        }
        #endregion




        public async Task ClientStartCountDown(int durationSeconds)
        {

            var endTimestamp = DateTimeOffset.UtcNow.AddSeconds(durationSeconds).ToUnixTimeMilliseconds();
            await Clients.All.SendAsync("ReceiveCountDown", endTimestamp);

            _isCountDownActiveOnCG = true;
        }

        public async Task StartYTVoting()
        {
            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} StartYTVoting: Voting Starting {"duration"} Secs.");
            if (_isYTVotingModeActive)
            {
                _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} StartYTVoting: Already Active, Return.");
                return;
            }

            _isYTVotingModeActive = true;

            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} StartYTVoting: Getting for accessToken...");
            var accessToken = await _ytoauthTokenService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogError("StartYTVoting: Access Token not found. Cannot start.");
                return;
            }
            else
            {
                _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} StartVotingMode: Getting accessToken...DONE");
            }




            var videoId = _configuration["YTVotingSettings:Video_id"];
            if (string.IsNullOrEmpty(videoId))
            {
                _logger.LogWarning("StartVotingMode: Video_id not found in config.");
                return;
            }




            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} StartVotingMode: Getting LiveChatId...");
            var _liveChatId = await _youtubeChatService.GetLiveChatIdAsync(videoId, accessToken);
            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} StartVotingMode: Getting LiveChatId...DONE " + _liveChatId);



            if (!string.IsNullOrEmpty(_liveChatId))
            {
                _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} StartVotingMode: Starting New Question...");
                await _ytAudienceVoteManager.StartNewQuestion(0, "შეკითხვა");
                _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} StartVotingMode: Starting New Question...DONE");

                _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} StartVotingMode: Sending Chat Message...");
                await _youtubeChatService.PostChatMessageAsync(_liveChatId, "ხმის მიცემა ჩართულია!", accessToken);
                _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} StartVotingMode: Sending Chat Message...DONE");

            }

            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} StartVotingMode: Method finished OK.");

        }


        public async Task StartVotingModeIndefinite()
        {
            if (_isYTVotingModeActive)
            {
                return;
            }

            _isYTVotingModeActive = true;
            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} StartVotingModeIndefinite: Voting Starting for undefined Duration.");
            var accessToken = await _ytoauthTokenService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogError("StartVotingModeIndefinite: Access Token not found. Cannot start.");
                return;
            }
            else
            {
                _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} StartVotingModeIndefinite: Getting accessToken...DONE");
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
            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} ხმის მიცემა შეჩერდა.");

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
            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} მომხმარებელი {authorChannelId} გაგდებულია.");

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
			await Task.CompletedTask;
		}

        public async Task StartYTDataCollectingAsync()
        {
            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} GameHub:StartYTDataCollectingAsync:  Going to star Collector Service.");
            _ = await _ytdataCollectorService.StartCollectingAsync();
        }
        public async Task SaveYTOAuthTokens(string accessToken, string refreshToken)
        {

            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} YouTube OAuth Tokens Saved in GameHub-ში.  accessToken {accessToken}, refreshToken {refreshToken}");
            // ვინახავთ ტოკენებს OAuthTokenService-ში
            await _ytoauthTokenService.SaveTokensAsync(accessToken, refreshToken);

            /////	დროებით აქ აღარ დავიწყებ
            _ = await _ytdataCollectorService.StartCollectingAsync();
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
                _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Successfully loaded {questionCount} questions from a file");
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

            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Client connected. ConnectionId: {Context.ConnectionId}, IP: {clientIp}, Name: {clientName}");

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
                        _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Client '{existingPlayer.Name}' from {existingPlayer.Ip} reconnected. Removed old entry with ConnectionId: {existingPlayer.ConnectionId}.");
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

                _ = _gameService.ConnectedPlayers.AddOrUpdate(Context.ConnectionId, newPlayer, (key, existingPlayer) => newPlayer);

                _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Client {Context.ConnectionId} '{isRegisteredClient.clientName}' from {clientIp} {isRegisteredClient.clientType} is REGISTERED. Total registered players: {_gameService.ConnectedPlayers.Count}");

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
                _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Client '{player.Name}' disconnected. Total registered players: {_gameService.ConnectedPlayers.Count}");
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
                _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Message from '{sender.Name}' sent to client '{connectionId}': {message}");
            }
            else
            {
                _logger.LogWarning($"Failed to send message: Client with Connection ID '{connectionId}' not found.");
            }
        }

       


        public async Task SendQuestion(QuestionModel question, int durationSeconds, GameMode mode, bool disableInput, List<Player>? clients)
        {
            _logger.LogInformation($"Sending question '{question.Question}' with a {durationSeconds}s countdown.");

            _activeQuestion = question;
            _answersReceivedCount = 0;
            _currentGameMode = mode;

            _midiLightingService.SendNoteOn(_midiSettings.CountDownNote, _midiSettings.CountDownVelocity);

            var endTimestamp = DateTimeOffset.UtcNow.AddSeconds(durationSeconds).ToUnixTimeMilliseconds();

            _activePlayerIds =// clients?.Select(c => c.ConnectionId).ToList() ?? 
                _gameService.ConnectedPlayers.Where(p => (p.Value.ClientType == "Contestant" || p.Value.ClientType == "Presenter")).Select(p => p.Key).ToList();

            var clientGroup = clients != null && clients.Count > 0
                ? Clients.Clients(_activePlayerIds)
                : Clients.All;

            await clientGroup.SendAsync("ReceiveQuestion", question, disableInput);
            if (_currentGameMode == GameMode.RapidMode) await clientGroup.SendAsync("ReceiveCountDown", endTimestamp);
            await Clients.Client(GetOperatorConnectionId()).SendAsync("ReceiveCountDown", endTimestamp);

            if (_currentGameMode == GameMode.RapidMode) await CGWSCountDown(CGTemplateEnums.CountDown.ToString(), durationSeconds, CountDownStopMode.Start, endTimestamp);
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

                        _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Updated score for player '{connectedPlayer.Name}' from {connectedPlayer.Score} to {incomingPlayer.Score}");
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
                _ = _gameService.ConnectedPlayers.TryGetValue(playerConnectionId, out var notActivePlayer);
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
                        _gameService.AddPoints(player, 1);
                        _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Player '{player.Name}' submitted a CORRECT answer. Score: {player.Score}");
                    }
                    else
                    {
                        _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Player '{player.Name}' submitted an INCORRECT answer.");
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
                        //if (_currentCountDownMode == CountDownMode.FirstAnswer)
                        if (_currentGameMode == GameMode.FirstAnswer)
                        {
                            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} active player have submitted an answer. Stopping countdown.");
                            await Clients.All.SendAsync("StopCountDown", CountDownStopMode.Pause.ToString()); // Send a signal to stop the countdown

                        }
                    }

                    // This is the old logic for non-RapidFire modes
                    if (_currentGameMode == GameMode.AllPlayersAnswered)
                    {
                        _answersReceivedCount++;
                        if (_answersReceivedCount >= _activePlayerIds.Count)
                        {
                            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} All active players have submitted an answer. Stopping countdown.");
                            await Clients.All.SendAsync("StopCountDown", CountDownStopMode.TimeUp.ToString()); // Send a signal to stop the countdown
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
            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Round has ended. Processing results.");

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
            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Rapid Fire: Sending question '{nextQuestion.Question}'");

            _activeQuestion = nextQuestion;
            _currentDisableInput = disableInput;

            // Send the new question to the active players
            await Clients.Clients(_activePlayerIds).SendAsync("ReceiveQuestion", nextQuestion, _currentDisableInput);
            await CGWSUpdateQuestionTemplateData(CGTemplateEnums.QuestionFull.ToString(), nextQuestion);
            await CGWSUpdateQuestionTemplateData(CGTemplateEnums.QuestionLower.ToString(), nextQuestion);
        }
        public async Task StartRapidFire(List<Player> clients, int durationSeconds, bool disableInput)
        {
            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Starting Rapid Fire mode.");
            //_isRapidFireActive = true; // Set the flag to true
            _currentGameMode = GameMode.RapidMode;
            _currentDisableInput = disableInput;



            _activePlayerIds = clients.Select(c => c.ConnectionId).ToList();// ?? _connectedPlayers.Where(p => p.Value.ClientType == "Contestant").Select(p => p.Key).ToList();

            // Set up the countdown for the entire Rapid Fire session
            var endTimestamp = DateTimeOffset.UtcNow.AddSeconds(durationSeconds).ToUnixTimeMilliseconds();
            await Clients.Clients(_activePlayerIds).SendAsync("ReceiveCountDown", endTimestamp);

            await Clients.Clients(GetOperatorConnectionId()).SendAsync("ReceiveCountDown", endTimestamp);


            await CGWSCountDown(CGTemplateEnums.CountDown.ToString(), durationSeconds, CountDownStopMode.Start, endTimestamp);

        }

        public async Task Uuups(LastAction _lastAction)
        {
            var activePlayerId = _activePlayerIds.FirstOrDefault();
            if (activePlayerId != null && _gameService.ConnectedPlayers.TryGetValue(activePlayerId, out var player))
            {
                if (_lastAction == LastAction.Correct)
                    player.AddScore(-1);
                else
                    player.AddScore(1);

                _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Corrected Score for '{player.Name}'. Score: {player.Score}");


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
                        _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} RapidFire No Input: Operator confirmed a correct answer for player '{player.Name}'. Score: {player.Score}");


                    }
                }
                else
                {
                    _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} RapidFire No Input: Operator confirmed an incorrect answer.");

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
            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Received Connection ID from service {message.ServiceName}, {message.ConnectionId}");

            // Sends the connection ID to all clients except the sender.
            return Clients.Others.SendAsync("ReceiveConnectionId", message);
        }


        public async Task PassMessageToYTVoteManager(MessageToYTManager msg)
        {
            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} GameHub: PassMessageToYTVoteManager");
            msg.SenderConnectionId = Context.ConnectionId;
            var destClientId = GetYTVoteMangerConnectionId();
            if (destClientId!= null)
                await Clients.Clients(destClientId).SendAsync("ReceiveMessageAsync", msg);
        }

        /// <summary>
        /// This method is called by the operator to start a voting process.
        /// </summary>
        /// <param name="message">The voting request message.</param>
        public async Task StartVoting(VoteRequestMessage message)
        {
            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} GameHub: Received a voting request from an operator: IsVotingActive = {message.IsVotingActive}");
            var destClientId = GetYTVoteMangerConnectionId();
			if (destClientId != null)
				await Clients.Clients(destClientId).SendAsync("ReceiveVoteRequest", message);
        }

        /// <summary>
        /// This method is called by the operator to stop a voting process.
        /// </summary>
        /// <param name="message">The voting request message.</param>
        public async Task StopVoting(VoteRequestMessage message)
        {
            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} GameHub: Received a voting request from an operator: IsVotingActive = {message.IsVotingActive}");
            var destClientId = GetYTVoteMangerConnectionId();
			if (destClientId != null)
				await Clients.Clients(destClientId).SendAsync("ReceiveVoteRequest", message);

        }
        public Task Ping()
        {

            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Received Ping from client: {Context.ConnectionId}");
            return Clients.Client(Context.ConnectionId).SendAsync("Pong");

        }


        public async Task ReceiveVoteResults(VoteResultsMessage message)
        {
            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} GameHub: Received vote results from YouTubeChatService");
            // This is where we would trigger the CasparCG template.
            // For now, we are just logging the received data.
            var chatMessageBuilder = new StringBuilder();


            _ = chatMessageBuilder.AppendLine($"🗳️ სულ დარეგისტრირდა {message.Results.Sum(x => x.Count)} პასუხი:");

            foreach (var result in message.Results)
            {
                _ = chatMessageBuilder.AppendLine($"   ✔️ {result.Answer}: {result.Count} ({result.Percentage:F2}%)");
            }

            _ = chatMessageBuilder.AppendLine("");
            _ = chatMessageBuilder.AppendLine("🙏 გმადლობთ მონაწილეობისთვის!");


            var operatorConnectionId = GetOperatorConnectionId();
            if (operatorConnectionId != null)
            {
                await Clients.Client(operatorConnectionId).SendAsync("VoteResultsMessage", message);
            }

            await CGWSYTVote(CGTemplateEnums.YTVote.ToString(), message);

        }
    }
}
