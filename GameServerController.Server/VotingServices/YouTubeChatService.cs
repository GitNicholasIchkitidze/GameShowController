
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GameController.Server.VotingServices
{
	public class YouTubeChatService : IYouTubeChatService
	{
		private readonly HttpClient _httpClient;
		private readonly ILogger<YouTubeChatService> _logger;
		private readonly IConfiguration _configuration;
		private readonly IYTOAuthTokenService _ytoauthTokenService;

		//private readonly string _ytapiKey;
		//private readonly string _ytrequesturl;


		public YouTubeChatService(HttpClient httpClient,
			IConfiguration configuration,
			IYTOAuthTokenService ytoauthTokenService,
			ILogger<YouTubeChatService> logger)
		{
			_httpClient = httpClient;
			_logger = logger;
			_configuration = configuration;
			_ytoauthTokenService = ytoauthTokenService;
			// API Key-ის მოძიება კონფიგურაციიდან
			//_ytapiKey = _configuration["YTVotingSettings:APIKey"] ?? "";
			//_ytrequesturl = _configuration["YTVotingSettings:requesturl"] ?? ""; 
		}
		public async Task<string?> GetLiveChatIdAsync(string videoId, string accessToken)
		{
			var requestUrl = $"https://www.googleapis.com/youtube/v3/videos?part=liveStreamingDetails&id={videoId}";

			try
			{
				// ავტორიზაციის ჰედერის დამატება accessToken-ის გამოყენებით
				_httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

				var response = await _httpClient.GetAsync(requestUrl);
				response.EnsureSuccessStatusCode();

				var jsonResponse = await response.Content.ReadAsStringAsync();
				using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
				{
					var items = doc.RootElement.GetProperty("items");
					if (items.GetArrayLength() > 0)
					{
						var liveChatId = items[0].GetProperty("liveStreamingDetails")
												 .GetProperty("activeLiveChatId").GetString();

						_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} GetLiveChatIdAsync: Successfully fetched Live Chat ID: {liveChatId}");
						return liveChatId;
					}
				}
			}
			catch (HttpRequestException httpEx)
			{
				var errorContent = httpEx.Message;
				_logger.LogError(httpEx, $"GetLiveChatIdAsync: HTTP request error fetching ID for video {videoId}. Details: {errorContent}");
			}
			catch (Exception ex)
			{
				_logger.LogError($"GetLiveChatIdAsync: General error fetching ID: {ex.Message}");
			}
			finally
			{
				// აუცილებელია ჰედერის გასუფთავება, რომ არ იმოქმედოს სხვა მოთხოვნებზე
				_httpClient.DefaultRequestHeaders.Authorization = null;
			}

			return null;
		}
		public  async Task<string?> GetLiveChatIdAsync_(string videoId)
		{
			var _ytapiKey = _configuration["YTVotingSettings:APIKey"] ?? "";
			var _ytrequesturl = $"https://www.googleapis.com/youtube/v3/videos?part=liveStreamingDetails&id={videoId}&key={_ytapiKey}";


			try
			{
				var requestUrl = 
								 $"{_ytrequesturl}" +
								 $"?part=liveStreamingDetails" +
								 $"&id={videoId}" +
								 $"&key={_ytapiKey}"; // აქ გამოიყენეთ თქვენი API გასაღები

				var response = await _httpClient.GetAsync(_ytrequesturl);
				response.EnsureSuccessStatusCode();

				var jsonResponse = await response.Content.ReadAsStringAsync();
				using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
				{
					var items = doc.RootElement.GetProperty("items");
					if (items.GetArrayLength() > 0)
					{
						var liveChatId = items[0].GetProperty("liveStreamingDetails")
												   .GetProperty("activeLiveChatId").GetString();
						return liveChatId;
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"GetLiveChatIdAsync: Error fetching ID: {ex.Message}");
			}
			return null; 
		}

		public async Task<string?> GetLiveChatMessagesAsync(string liveChatId, string? pageToken = null)
		{
			try
			{	// ტოკენის მიღება OAuthTokenService-დან
				var accessToken = await _ytoauthTokenService.GetAccessTokenAsync();
				if (string.IsNullOrEmpty(accessToken))
				{
					_logger.LogError("GetLiveChatMessagesAsync: Access Token Not Found.");
					return null;
				}

				// YouTube API-ს URL-ი
				var requestUrl = $"https://www.googleapis.com/youtube/v3/liveChat/messages?" +
								 $"liveChatId={liveChatId}&" +
								 $"part=snippet,authorDetails&" +
								 $"maxResults=200";



				// თუ pageToken არსებობს, დაამატეთ URL-ს
				if (!string.IsNullOrEmpty(pageToken))
				{
					requestUrl += $"&pageToken={pageToken}";
				}

	
				



				// მოთხოვნის ავტორიზაცია
				_httpClient.DefaultRequestHeaders.Authorization =
					new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

				// API მოთხოვნის გაგზავნა
				var response = await _httpClient.GetAsync(requestUrl);
				response.EnsureSuccessStatusCode();

				return await response.Content.ReadAsStringAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError($"GetLiveChatMessagesAsync: Error While receiving chat message: {ex.Message}");
				return null;
			}
		}


		public async Task PostChatMessageAsync(string liveChatId, string message, string accessToken)
		{
			try
			{
				var _ytapiKey = _configuration["YTVotingSettings:APIKey"] ?? "";
				var requestUri = $"https://www.googleapis.com/youtube/v3/liveChat/messages?part=snippet,authorDetails&key={_ytapiKey}";

				// მოთხოვნის Body-ის შექმნა
				var requestBody = new JObject(
					new JProperty("snippet",
						new JObject(
							new JProperty("liveChatId", liveChatId),
							new JProperty("type", "textMessageEvent"),
							new JProperty("textMessageDetails",
								new JObject(
									new JProperty("messageText", message)
								)
							)
						)
					)
				);

				_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
				var content = new StringContent(requestBody.ToString(), Encoding.UTF8, "application/json");

				var response = await _httpClient.PostAsync(requestUri, content);

				if (response.IsSuccessStatusCode)
				{
					_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} PostChatMessageAsync: Message sent to Live chat: {message}");
				}
				else
				{
					var error = await response.Content.ReadAsStringAsync();
					_logger.LogError($"{Environment.NewLine}PostChatMessageAsync: Error while Message Send: {response.ReasonPhrase}, Details: {error}");
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"PostChatMessageAsync: Error while Message Send: {ex.Message}");
			}
		}

		public async Task DeleteChatMessageAsync(string messageId, string accessToken)
		{
			try
			{
				var requestUrl = $"https://www.googleapis.com/youtube/v3/liveChat/messages?id={messageId}";

				_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
				var response = await _httpClient.DeleteAsync(requestUrl);

				if (response.IsSuccessStatusCode)
				{
					_logger.LogInformation($"DeleteChatMessageAsync: Chat Message {messageId} Deleted.");
				}
				else
				{
					_logger.LogWarning($"DeleteChatMessageAsync: Error while deleting chat Message: {response.ReasonPhrase}");
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"შეცდომა შეტყობინების წაშლისას: {ex.Message}");
			}
		}
	}
}
