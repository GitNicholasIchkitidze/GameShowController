using GameController.Server.VotingManagers;
using GameController.Shared.Models;
using System.Text.Json;
namespace GameController.Server.VotingServices
{
    public class YouTubeDataCollectorService : IYouTubeDataCollectorService
    {


        private readonly ILogger<YouTubeDataCollectorService> _logger;
        private readonly IYouTubeChatService _youtubeChatService;
        private readonly IYTAudienceVoteManager _ytAudienceVoteManager;
        private readonly IYTOAuthTokenService _ytoauthTokenService;
        private readonly IConfiguration _configuration;

        private string? _liveChatId;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly int _pollingIntervalSeconds = 5;


        public YouTubeDataCollectorService(ILogger<YouTubeDataCollectorService> logger,
            IConfiguration configuration,
            IYouTubeChatService youtubeChatService,
            IYTAudienceVoteManager ytAudienceVoteManager,
            IYTOAuthTokenService ytoauthTokenService
            )
        {
            _logger = logger;
            _youtubeChatService = youtubeChatService;
            _ytAudienceVoteManager = ytAudienceVoteManager;
            _ytoauthTokenService = ytoauthTokenService;
            _configuration = configuration;


        }



        public async Task<OperationResult> StartCollectingAsync()
        {
            // ეს მეთოდი დაიწყებს კოლექციის პროცესს, როდესაც მომხმარებელი გაივლის ავტორიზაციას.
            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} StartCollectingAsync:  YouTube Data Collector Service starting.");
            _cancellationTokenSource = new CancellationTokenSource();

            var result = new OperationResult(true);


            try
            {
                var videoId = _configuration["YTVotingSettings:Video_id"] ?? "";

                if (string.IsNullOrEmpty(videoId))
                {
                    _logger.LogError($"StartCollectingAsync:  YouTube Live Video ID not found in config.");
                    result.SetError("StartCollectingAsync: YouTube ვიდეოს ID კონფიგურაციაში ვერ მოიძებნა.");
                    return result;
                }

                var accessToken = await _ytoauthTokenService.GetAccessTokenAsync();

                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogError("StartCollectingAsync: Access Token not found. Cannot start collecting data.");
                    result.SetError("StartCollectingAsync: Access Token not found.");
                    return result;
                }

                var liveChatId = await _youtubeChatService.GetLiveChatIdAsync(videoId, accessToken);




                if (string.IsNullOrEmpty(_liveChatId))
                {
                    _logger.LogError($"StartCollectingAsync:  YouTube Live chat ID Not found. for video {videoId}");
                    result.SetError($"StartCollectingAsync:  ვიდეოს ID {videoId}-ით YouTube ლაივ ჩატი ვერ მოიძებნა");
                    return result;
                }

                //if (!GameController.Server.Hubs.GameHub._isYTVotingModeActive)
                //{
                // ვიწყებთ მონაცემების შეგროვების ციკლს ახალ Task-ში. თუ ეს უკვე აქტიურია, არ ვიწყებთ ახალს.
                await Task.Run(() => ExecuteAsync(_cancellationTokenSource.Token));
                result.SetError($"StartCollectingAsync:  YouTube Data Collector Started.");

                //}

            }
            catch (Exception ex)
            {
                _logger.LogError($"StartCollectingAsync:  YouTube Date Collector Error: {ex.Message}");
                result.SetError($"StartCollectingAsync:  კოლექციის დაწყების შეცდომა: {ex.Message}");
            }
            return result;
        }

        public void StopCollecting()
        {
            _cancellationTokenSource?.Cancel();
            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} YouTube Data Collector Service-ის კოლექცია შეჩერდა.");

        }

        private async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string pageToken = null;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var chatResponseJson = await _youtubeChatService.GetLiveChatMessagesAsync(_liveChatId, pageToken);

                    // ... თქვენი არსებული კოდი, რომელიც სწორად ამუშავებს JSON-ს და pageToken-ს.
                    if (string.IsNullOrEmpty(chatResponseJson))
                    {
                        _logger.LogWarning("ცარიელი პასუხი YouTube-დან. შეტყობინებები არ არის.");
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                        continue;
                    }

                    var jsonDoc = JsonDocument.Parse(chatResponseJson);

                    if (!jsonDoc.RootElement.TryGetProperty("items", out var chatMessages) || chatMessages.ValueKind != JsonValueKind.Array)
                    {
                        _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} არ არის ახალი შეტყობინებები.");
                        if (jsonDoc.RootElement.TryGetProperty("nextPageToken", out var nextPageTokenElement))
                        {
                            pageToken = nextPageTokenElement.GetString();
                        }
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                        continue;
                    }

                    foreach (var message in chatMessages.EnumerateArray())
                    {
                        var messageText = message.GetProperty("snippet").GetProperty("displayMessage").GetString();
                        var authorChannelId = message.GetProperty("authorDetails").GetProperty("channelId").GetString();
                        var authorName = message.GetProperty("authorDetails").GetProperty("displayName").GetString();
                        var formattedAnswer = messageText?.Trim().ToUpper();

                        var audienceMember = new AudienceMember { AuthorChannelId = authorChannelId, AuthorName = authorName, Answer = formattedAnswer, Platform = "YouTube" };
                        var accessToken = await _ytoauthTokenService.GetAccessTokenAsync();
                        if (formattedAnswer == "A" || formattedAnswer == "B" || formattedAnswer == "C" || formattedAnswer == "D")
                        {
                            await _ytAudienceVoteManager.ProcessVoteAsync(audienceMember, accessToken, _liveChatId, "თქვენი პასუხი რეგისტრირებულია");
                        }
                        else
                        {
                            await _ytAudienceVoteManager.ProcessVoteAsync(audienceMember, accessToken, _liveChatId, "თქვენი არ არის რეგისტრირებული");
                        }

                    }

                    if (jsonDoc.RootElement.TryGetProperty("nextPageToken", out var nextPageTokenElementAfter))
                    {
                        pageToken = nextPageTokenElementAfter.GetString();
                    }
                    else
                    {
                        _logger.LogWarning("PostChatMessageAsync: nextPageToken Not found in Answer.");
                        pageToken = null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"PostChatMessageAsync: while Data manipulation: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}

