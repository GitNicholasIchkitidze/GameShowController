using GameController.FBService.Extensions;
using GameController.FBService.Models;
using GameController.FBService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class FacebookWebhooksController : ControllerBase
{
	private readonly IConfiguration _configuration;
	private readonly HttpClient _httpClient;
	private readonly ILogger<FacebookWebhooksController> _logger;

	private readonly IRateLimitingService _rateLimitingService;


	private readonly ApplicationDbContext _dbContext;

	private List<ClientConfiguration> _registeredClients;
	private readonly string _imageFolderPath;
	private readonly string _voteStartFlag;
	private readonly string _pageAccessToken;
	private readonly string _verifyToken;
	private readonly int _voteMinuteRange;
	private readonly IMessageQueueService _messageQueueService;


	public FacebookWebhooksController(ILogger<FacebookWebhooksController> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory, ApplicationDbContext dbContext, IRateLimitingService rateLimitingService, IMessageQueueService messageQueueService)
	{
		//_httpClient = new HttpClient();
		_logger = logger;
		_configuration = configuration;
		_registeredClients = _configuration.GetSection("RegisteredClients").Get<List<ClientConfiguration>>() ?? new List<ClientConfiguration>();
		_voteStartFlag = _configuration.GetValue<string>("voteStartFlag") ?? "";
		_imageFolderPath = _configuration.GetValue<string>("imageFolderPath") ?? "";
		_pageAccessToken = _configuration.GetValue<string>("pageAccessToken") ?? "";
		_verifyToken = _configuration.GetValue<string>("verifyToken") ?? "";
		_voteMinuteRange = _configuration.GetValue<int>("voteMinuteRange", 5);

		_rateLimitingService = rateLimitingService;

		_httpClient = httpClientFactory.CreateClient();
		_dbContext = dbContext;
		_messageQueueService = messageQueueService;
	}

	[HttpGet]
	public IActionResult VerifyWebhook([FromQuery(Name = "hub.mode")] string mode,
									   [FromQuery(Name = "hub.verify_token")] string token,
									   [FromQuery(Name = "hub.challenge")] string challenge)
	{
		if (mode == "subscribe" && token ==	_verifyToken)
		{
			_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Verified WebHook");
			return Ok(challenge);
		}

		return BadRequest("Verification failed.");
	}

	[HttpPost]
	public async Task<IActionResult> HandleWebhook([FromBody] JsonObject payload)
	{
		_logger.LogInformationWithCaller($"PayLoad Received: {payload}");

		try
		{
			// CRITICAL STEP: Offload the raw payload string to the worker queue
			await _messageQueueService.EnqueueMessageAsync(payload.ToString());
		}
		catch (Exception ex)
		{
			// Log the queuing failure but still return 200 OK to Facebook
			_logger.LogError($"Failed to enqueue message: {ex.Message}");
		}

		// MUST return 200 OK immediately to satisfy Facebook's 20-second timeout.
		return Ok();
	}
	[HttpPost]



	//[HttpPost]
	private async Task<IActionResult> HandleWebhook_Old([FromBody] JsonObject payload)
	{
		_logger.LogInformationWithCaller($"Entered in HandleWebHook");
		_logger.LogInformationWithCaller($"PayLoad {payload}");




		try
		{
			// ვიღებთ დატვირთვას
			var entry = payload["entry"]?.AsArray();
			if (entry == null)
			{
				_logger.LogErrorWithCaller($"invalid payload");
				return Ok();
			}

			foreach (var pageEntry in entry)
			{
				_logger.LogInformationWithCaller($"foreach in entry: {pageEntry}");

				var changes = pageEntry["changes"]?.AsArray();
				var messaging = pageEntry["messaging"]?.AsArray();

				// ვამოწმებთ, არის თუ არა ეს ცვლილება პოსტზე (კომენტარი, რეაქცია)
				if (changes != null)
				{
					foreach (var change in changes)
					{
						var field = change["field"]?.GetValue<string>();
						var value = change["value"]?.AsObject();

						if (field == "feed" && value != null)
						{
							var verb = value["verb"]?.GetValue<string>();
							var fromUser = value["from"]?.AsObject();
							var message = value["message"]?.GetValue<string>();
							var reactionType = value["reaction_type"]?.GetValue<string>();

							// აქ დაამატეთ თქვენი დამუშავების ლოგიკა
							// მაგალითად: if (verb == "add") { ... }
							_logger.LogInformation($"{Environment.NewLine}{DateTime.Now} User: {fromUser?["name"]}, Comment: {message}");
						}
					}
				}

				// ვამოწმებთ, არის თუ არა ეს შეტყობინება Messenger-დან
				if (messaging != null)
				{
					foreach (var messageObject in messaging)
					{

						_logger.LogInformationWithCaller($"foreach in messaging: {messageObject}");

						var senderId = messageObject?["sender"]?["id"]?.GetValue<string>();
						var receivedMessage = messageObject?["message"]?["text"]?.GetValue<string>();
						var timeStamp = messageObject?["timestamp"]?.GetValue<long>();
						var postBack = messageObject?["postback"]?["payload"]?.GetValue<string>();
						var senderName = await GetUserNameAsync(senderId);

						var lastVote = await _dbContext.FaceBookVotes
														.Where(v => v.UserId == senderId && v.Message != _voteStartFlag)
														.OrderByDescending(v => v.Timestamp)
														.FirstOrDefaultAsync();
						var canVote = (lastVote == null || (lastVote != null && (DateTime.Now - lastVote.Timestamp).TotalMinutes >= _voteMinuteRange));
						_logger.LogInformationWithCaller($"Checked for Last vote");


						// აქ დაამატეთ თქვენი Messenger-ის ლოგიკა
						if (!string.IsNullOrEmpty(senderId) && (!string.IsNullOrEmpty(receivedMessage) || !string.IsNullOrEmpty(postBack)))
						{
							// მომხმარებლის ID-ს ამოსაღებად
							// ვუგზავნით ავტომატურ პასუხს
							var localTime = UnixTimeStampToDateTime(timeStamp.Value);

							if (receivedMessage?.ToLower() == _voteStartFlag)
							{
								_logger.LogInformationWithCaller($"FbUser: {senderId} Requested enable vote");
								
								var imageUrls = _registeredClients.Select(x => _imageFolderPath + x.image).ToList();
								var names = _registeredClients.Select(x => x.clientName).ToList();
								if (canVote)
								{
									 await SendImageGalleryAsyncWithButtons(senderId, imageUrls, names);
									var timestamp = DateTime.Now;
									var newVote = new Vote
									{
										Timestamp = timestamp,
										Id = $"{senderId}.{timestamp.ToString("yyyyMMddHHmmssfff")}",
										UserName = senderName,
										UserId = senderId,
										CandidateName = string.Empty,

										Message = receivedMessage ?? postBack ?? string.Empty
									};
									await _dbContext.FaceBookVotes.AddAsync(newVote);
									await _dbContext.SaveChangesAsync();
								}
									
								else
									_logger.LogInformationWithCaller($"Not Allowed For Vote till {lastVote?.Timestamp.AddMinutes(_voteMinuteRange).ToString("yyyy-MM-dd HH:mm:ss.fff")}  last vote Date: {lastVote?.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")}");


							}
							else if (!string.IsNullOrEmpty(senderId) && (string.IsNullOrEmpty(receivedMessage) || !string.IsNullOrEmpty(postBack)))
							{
								_logger.LogInformationWithCaller($"FbUser: {senderId} Sent Something");
								var names = _registeredClients.Select(x => x.clientName).ToList();
								var isValidAnswer = names.FirstOrDefault(c => c == postBack);

								
								if (isValidAnswer != null)
								{

									_logger.LogInformationWithCaller($"FbUser: ({senderId}) {senderName}'s Message has Valid Answer");



									if (canVote)
									{
										var timestamp = DateTime.Now;
										var newVote = new Vote
										{
											Timestamp = timestamp,
											Id = $"{senderId}.{timestamp.ToString("yyyyMMddHHmmssfff")}",
											UserName = senderName,
											UserId = senderId,
											CandidateName = postBack,

											Message = receivedMessage ?? postBack ?? string.Empty
										};
										await _dbContext.FaceBookVotes.AddAsync(newVote);
										await _dbContext.SaveChangesAsync();

										_logger.LogInformationWithCaller($"Answer Accepted, Saved Vote");
										await SendMessageAsync(senderId, $"თქვენი შეტყობინება მიღებულია! თქვენ მიეცით ხმა {postBack}ს, რეგისტრაციის ID: {newVote.Id}");
									}
									else
									{
										await SendMessageAsync(senderId, $"თქვენ არ შეგიძლიათ ხმის მიცემა {lastVote?.Timestamp.AddMinutes(_voteMinuteRange).ToString("yyyy-MM-dd HH:mm")} -მდე, მადლობთ რომ ცადეთ");
										_logger.LogInformationWithCaller($"Not Allowed For Vote till {lastVote?.Timestamp.AddMinutes(_voteMinuteRange).ToString("yyyy-MM-dd HH:mm:ss.fff")}  last vote Date: {lastVote?.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")}");
									}



								}
							}
								

							
							
							_logger.LogInformationWithCaller($"Messenger User: {senderName} ({senderId}), Message: {receivedMessage} Date {localTime}");
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			// შეცდომების დამუშავება
			_logger.LogInformation($"Error processing webhook: {ex.Message}");
		}

		return Ok();
	}


	
	

	private DateTime UnixTimeStampToDateTime(double unixTimeStampInMilliseconds)
	{
		// Unix timestamp-ის საწყისი თარიღი
		DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);

		// ვუმატებთ მიღებულ მილიწამებს საწყის თარიღს
		dtDateTime = dtDateTime.AddMilliseconds(unixTimeStampInMilliseconds).ToLocalTime();

		return dtDateTime;
	}

	
	

	
	private async Task<string> GetUserNameAsync(string userId)
	{
		try
		{
			var url = $"https://graph.facebook.com/v18.0/{userId}?fields=first_name,last_name&access_token={_pageAccessToken}";
			var response = await _httpClient.GetAsync(url);
			response.EnsureSuccessStatusCode();

			var limit = _rateLimitingService.LogApiCall();

			var jsonString = await response.Content.ReadAsStringAsync();
			var jsonNode = JsonNode.Parse(jsonString);

			var firstName = jsonNode?["first_name"]?.GetValue<string>();
			var lastName = jsonNode?["last_name"]?.GetValue<string>();

			return $"{firstName} {lastName}";
		}
		catch (HttpRequestException ex)
		{
			_logger.LogInformation($"Error calling Graph API: {ex.Message}");
			return "უცნობი მომხმარებელი";
		}
	}

	private async Task SendMessageAsync(string recipientId, string messageText)
	{
		try
		{
			var jsonPayload = new
			{
				recipient = new { id = recipientId },
				message = new { text = messageText }
			};

			var content = new StringContent(
				Newtonsoft.Json.JsonConvert.SerializeObject(jsonPayload),
				System.Text.Encoding.UTF8,
				"application/json"
			);

			var limit = _rateLimitingService.LogApiCall();

			var url = $"https://graph.facebook.com/v18.0/me/messages?access_token={_pageAccessToken}";
			var response = await _httpClient.PostAsync(url, content);
			response.EnsureSuccessStatusCode();

			_logger.LogInformation($"Message sent successfully to {recipientId}.");
		}
		catch (HttpRequestException ex)
		{
			_logger.LogInformation($"Error sending message to {recipientId}: {ex.Message}");
		}
	}


	private async Task SendImageGalleryAsync(string recipientId, List<string> imageUrls)
	{
		try
		{
			var elements = new List<object>();

			foreach (var iUrl in imageUrls)
			{
				elements.Add(new
				{
					title = "ნახე ეს სურათი",
					image_url = iUrl,
					buttons = new[]
					{
					new
					{
						type = "web_url",
						url = iUrl,
						title = "გახსენი სრულ ეკრანზე"
					}
				}
				});
			}

			var jsonPayload = new
			{
				recipient = new { id = recipientId },
				message = new
				{
					attachment = new
					{
						type = "template",
						payload = new
						{
							template_type = "generic",
							elements = elements
						}
					}
				}
			};

			var content = new StringContent(
				Newtonsoft.Json.JsonConvert.SerializeObject(jsonPayload),
				System.Text.Encoding.UTF8,
				"application/json"
			);

			var url = $"https://graph.facebook.com/v18.0/me/messages?access_token={_pageAccessToken}";
			var response = await _httpClient.PostAsync(url, content);
			response.EnsureSuccessStatusCode();

			Console.WriteLine($"Image gallery sent successfully to {recipientId}.");
		}
		catch (HttpRequestException ex)
		{
			Console.WriteLine($"Error sending image gallery to {recipientId}: {ex.Message}");
		}
	}

	private async Task SendImageGalleryAsyncWithButtons(string recipientId, List<string> imageUrls, List<string> names)
	{
		try
		{
			var elements = new List<object>();

			for (int i = 0; i < imageUrls.Count; i++)
			{
				var name = names[i];
				var imageUrl = imageUrls[i];

				elements.Add(new
				{
					//title = name,
					image_url = imageUrl,
					buttons = new[]
					{
					new
					{
						type = "postback",
						title = $"შენი ხმა {name}-ს",
						payload = $"{name}"
					}
				}
				});
			}

			var jsonPayload = new
			{
				recipient = new { id = recipientId },
				message = new
				{
					attachment = new
					{
						type = "template",
						payload = new
						{
							template_type = "generic",
							elements = elements
						}
					}
				}
			};

			var content = new StringContent(
				Newtonsoft.Json.JsonConvert.SerializeObject(jsonPayload),
				System.Text.Encoding.UTF8,
				"application/json"
			);

			var limit = _rateLimitingService.LogApiCall();

			var url = $"https://graph.facebook.com/v18.0/me/messages?access_token={_pageAccessToken}";
			var response = await _httpClient.PostAsync(url, content);
			response.EnsureSuccessStatusCode();

			

			_logger.LogInformation($"Image gallery sent successfully to {recipientId}.");
		}
		catch (HttpRequestException ex)
		{
			_logger.LogError($"Error sending image gallery to {recipientId}: {ex.Message}");
		}
	}
}