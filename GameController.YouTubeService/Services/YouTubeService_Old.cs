using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Threading.Tasks;

namespace GameController.YouTubeService.Services
{

	public class YouTubeService_Old : IYouTubeService
	{
		private readonly Google.Apis.YouTube.v3.YouTubeService _youtubeService;
		private readonly TokenManagerService _tokenManager; // დავამატეთ TokenManagerService

		private readonly IConfiguration _configuration;
		private readonly ILogger<YouTubeService_Old> _logger;


		private readonly string _videoId;
		private readonly string _clientId;
		private readonly string _clientSecret;

		

		public YouTubeService_Old(ILogger<YouTubeService_Old> logger, IConfiguration configuration, TokenManagerService tokenManager)
		{

			_configuration = configuration;
			_logger = logger;
			_logger = logger;
			_videoId = configuration["YouTubeVideoId"];
			_clientId = configuration["YouTubeClientId"];
			_clientSecret = configuration["YouTubeClientSecret"];

			_tokenManager = tokenManager;


			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} >>> YouTubeService initialized <<<");

			string[] scopes = { Google.Apis.YouTube.v3.YouTubeService.Scope.YoutubeForceSsl };
			//TODO: Change to use a credential store and not a file
			//TODO: The app has to be logged in with a YouTube account
			UserCredential credential;
			try
			{
				using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
				{
					credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
						GoogleClientSecrets.FromStream(stream).Secrets,
						scopes,
						"user",
						CancellationToken.None
					).Result;
				}


			_youtubeService = new Google.Apis.YouTube.v3.YouTubeService(new BaseClientService.Initializer()
			{
				ApiKey = _configuration["YouTubeApiKey"],
				HttpClientInitializer = credential,
				ApplicationName = "Game Controller"
			});
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} OAuth Done");
			}
			catch (Exception ex)
			{

				_logger.LogError($"{Environment.NewLine}{DateTime.Now} Message Not Sent to LiveChat. {ex}");
			}

		}

		public async Task<string> GetLiveChatIdAsync(string videoId)
		{
			var request = _youtubeService.Videos.List("liveStreamingDetails");
			request.Id = videoId;
			var response = await request.ExecuteAsync();

			return response.Items[0].LiveStreamingDetails.ActiveLiveChatId;
		}

		public async Task<LiveChatMessageListResponse> GetLiveChatMessagesAsync_(string liveChatId, string? pageToken = null)
		{
			var request = _youtubeService.LiveChatMessages.List(liveChatId, "snippet,authorDetails");
			request.PageToken = pageToken;
			var response = await request.ExecuteAsync();

			return response;
		}

		public async Task<LiveChatMessage> SendLiveChatMessageAsync(string liveChatId, string messageText)
		{

			bool quotaSaveMode = _configuration.GetValue("YouTubeQuotaSaveMode",true);
			
			var liveChatMessage = new LiveChatMessage()
			{
				Snippet = new LiveChatMessageSnippet()
				{
					LiveChatId = liveChatId,
					Type = "textMessageEvent",
					TextMessageDetails = new LiveChatTextMessageDetails()
					{
						MessageText = messageText
					}
				}
			};

			if (quotaSaveMode)
			{
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Quota Save Mode is ON. Message Not Sent to LiveChat.");
				return liveChatMessage;
			}


			// Sends the message to the live chat using the YouTube Data API.
			var request = _youtubeService.LiveChatMessages.Insert(liveChatMessage, "snippet");
			try
			{
				var response = await request.ExecuteAsync();
				_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Message Sent to LiveChat.");

				return response;
			}
			catch (Exception ex)
			{

				_logger.LogError($"{Environment.NewLine}{DateTime.Now} Message Not Sent to LiveChat. {ex}");
				return liveChatMessage;
			}
		}


		public async Task<LiveChatMessageListResponse> GetLiveChatMessagesAsync(string liveChatId, string? pageToken = null)
		{
			// აქ ყველა მოთხოვნა უნდა იყოს დაცული try-catch ბლოკით
			try
			{
				// ყოველ ახალ მოთხოვნაზე განახლდება ტოკენი
				_youtubeService.HttpClientInitializer = new AccessTokenCredential(await _tokenManager.GetAccessTokenAsync());

				var request = _youtubeService.LiveChatMessages.List(liveChatId, "snippet,authorDetails");
				request.PageToken = pageToken;
				var response = await request.ExecuteAsync();

				return response;
			}
			catch (Google.GoogleApiException ex)
			{
				// მაგალითად, 401 Unauthorized -ის დამუშავება
				if (ex.Error.Code == 401)
				{
					_logger.LogWarning("Token expired or unauthorized. Trying to refresh token...");
					await _tokenManager.GetAccessTokenAsync();
					// ვცადოთ ხელახლა
					return await GetLiveChatMessagesAsync(liveChatId, pageToken);
				}
				throw; // სხვა შეცდომებს გადავცემთ
			}
		}
		public async Task<Channel> GetChannelDetailsAsync(string channelId)
		{
			////var request = _youTubeService.Channels.List("snippet");
			////request.Id = channelId;
			////var response = await request.ExecuteAsync();
			////return response.Items.Count > 0 ? response.Items[0] : null;
			return await Task.FromResult(new Channel());
		}
	}
}
