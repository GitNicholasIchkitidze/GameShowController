using Azure;
using GameController.FBService.Extensions;
using GameController.FBService.Heplers;
using GameController.FBService.Models;
using GameController.Shared.Models;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace GameController.FBService.Services
{
	public class WebhookProcessorService : IWebhookProcessorService
	{
		private readonly ILogger<WebhookProcessorService> _logger;
		private readonly ApplicationDbContext _dbContext;
		private readonly ICacheService _cacheService; // NEW: Redis service
		private readonly IRateLimitingService _rateLimitingService; // Existing service, now Redis-backed
		private readonly HttpClient _httpClient; // HttpClient from the original controller
												 //ISignalRClient signalRClient,


		
		private readonly IDempotencyService _dempotencyService;

		// Configuration fields moved from the controller
		private readonly List<FBClientConfiguration> _registeredClients;
		private readonly string _voteStartFlag;
		private readonly string _pageAccessToken;
		private readonly string _imageFolderPath;
        private readonly string _defaultCandidateImageUrl;
        private readonly int _voteMinuteRange;
		private readonly IGlobalVarsKeeper _varsKeeper;

		public WebhookProcessorService(ILogger<WebhookProcessorService> logger,
			ApplicationDbContext dbContext,
			ICacheService cacheService,
			IRateLimitingService rateLimitingService,
			IConfiguration configuration,
			IHttpClientFactory httpClientFactory,
			IDempotencyService dempotencyService,
			IGlobalVarsKeeper varsKeeper
			)
		{
			_logger = logger;
			_dbContext = dbContext;
			_cacheService = cacheService;
			_rateLimitingService = rateLimitingService;
			_dempotencyService = dempotencyService;
			_varsKeeper = varsKeeper;

			// Initialization of configuration fields (moved from controller)
			_registeredClients = configuration.GetSection("RegisteredClients").Get<List<FBClientConfiguration>>() ?? new List<FBClientConfiguration>();
			_voteStartFlag = configuration.GetValue<string>("voteStartFlag") ?? "";
			_imageFolderPath = configuration.GetValue<string>("imageFolderPath") ?? "";
			_pageAccessToken = configuration.GetValue<string>("pageAccessToken") ?? "";
			_voteMinuteRange = configuration.GetValue<int>("voteMinuteRange", 5);
            _defaultCandidateImageUrl = configuration.GetValue<string>("defaultCandidateImageUrl","");

            _httpClient = httpClientFactory.CreateClient();


		}

		// ----------------------------------------------------
		// CORE WEBHOOK PROCESSING METHOD
		// ----------------------------------------------------


		public async Task<OperationResult> ProcessWebhookMessageAsync(string rawPayload)
		{
			var res = new OperationResult(true);
			var payload = JsonNode.Parse(rawPayload)?.AsObject();
			var messageType = ExtractMessageType(payload);
			var messageId = ExtractMessageId(payload, messageType);
			
			var senderId = ExtractMessageSenderOrRecipientId(payload, "sender");
			var recipientId = ExtractMessageSenderOrRecipientId(payload, "recipient");
			var userName = await GetUserNameAsync(senderId, recipientId);

			if (senderId == recipientId)
			{
				_logger.LogWarningWithCaller("Sender ID is the same as Recipient ID. Ignoring self-sent message.");
				res.Message = "Ignored self-sent message.";
				return res;
			}
				

			switch (messageType)
			{
				case "message":
					if (string.IsNullOrEmpty(senderId)) break;
					var messageText = ExtractMessageText(payload);
					if (!string.IsNullOrEmpty(messageText))
					{
						res = await ProcessTextMessageAsync(senderId, recipientId, messageId, userName, messageText);
					}
					break;
				case "postback":							

					if (string.IsNullOrEmpty(senderId)) break;
					var postbackPayload = ExtractMessagePostbackPayLoad(payload);
					if (!string.IsNullOrEmpty(postbackPayload))
					{
						res = await ProcessPostbackAsync(senderId, recipientId, messageId, userName, postbackPayload);
					}

					break;
				case "reaction":
					res = await HandleReactionEvent(payload);
					break;
			}
			return res;

		}


		/// <summary>
		/// Handles the business logic for a reaction event.
		/// </summary>
		private async Task<OperationResult> HandleReactionEvent(JsonObject payload)
		{
			// 1. Safely extract the reaction details
			var res = new OperationResult(true);
			var messagingEvent = payload?["entry"]?.AsArray().FirstOrDefault()?
										.AsObject()?["messaging"]?.AsArray().FirstOrDefault()?
										.AsObject();

			var reactionObject = messagingEvent?["reaction"]?.AsObject();
			if (reactionObject == null)
			{
				_logger.LogWarningWithCaller("Could not find reaction object in payload.");
				res.Result = false;
				return res;
			}

			string? mid = reactionObject["mid"]?.GetValue<string>();
			string? action = reactionObject["action"]?.GetValue<string>();
			string? emoji = reactionObject["reaction"]?.GetValue<string>();

			if (string.IsNullOrEmpty(mid) || string.IsNullOrEmpty(action) || string.IsNullOrEmpty(emoji))
			{
				_logger.LogWarningWithCaller("Reaction payload was missing required fields (mid, action, or emoji).");
				res.Result = false;
				return res;
			}

			_logger.LogInformationWithCaller($"Processing reaction: User '{action}' with '{emoji}' on message '{mid}'");

			// 2. Implement your business logic
			if (action == "react")
			{
				// TODO: Find the message in your database using the 'mid'.
				// TODO: Record that a user added this specific 'emoji'.
				// Example: await _myDatabase.AddReactionAsync(messageId: mid, reaction: emoji);
			}
			else if (action == "unreact")
			{
				// TODO: Find the message in your database using the 'mid'.
				// TODO: Record that a user removed this specific 'emoji'.
				// Example: await _myDatabase.RemoveReactionAsync(messageId: mid, reaction: emoji);
			}
			return res;
		}

		



		private async Task<OperationResult> ProcessTextMessageAsync(string senderId, string recipientId, string messageId, string userName, string text)
		{
			var result = new OperationResult(true);
			if (text.Equals(_voteStartFlag, StringComparison.OrdinalIgnoreCase))
			{


				var lockKeyForVote = $"vote:{senderId}:{recipientId}";
				var lockKeyForEnableVote = $"NeedForVote:{senderId}:{recipientId}";
				var isLockedKeyForVote = await _cacheService.GetAcquiredLockAsync(lockKeyForVote, TimeSpan.FromMinutes(_voteMinuteRange));
				var isLockedKeyForEnableVote = await _cacheService.GetAcquiredLockAsync(lockKeyForEnableVote, TimeSpan.FromMinutes(_voteMinuteRange));
				if (!isLockedKeyForVote && !isLockedKeyForEnableVote)
				{
					// 1. Get image and name lists
					var imageUrls = new List<string>(); // Populate this list with actual image URLs
					var names = new List<string>(); // Populate this list with actual client names

					foreach (var client in _registeredClients)
					{
						// NOTE: Assuming your ImageFolderPath is a base URL/path
						var imageUrl = $"{_imageFolderPath}/{client.image}";
						imageUrls.Add(imageUrl);
						names.Add(client.clientName);
					}

					// 2. Send Gallery (Rate-limited)
					result = await SendImageGalleryAsyncWithButtons(senderId, recipientId, messageId, userName, imageUrls, names);
					return result;
				}
				else
				{
					_logger.LogWarningWithCaller($"Vote Start Request from {senderId} Not Processed due to rate limit ({_voteMinuteRange} min). ");

					// ❌ მოთხოვნისამებრ: დავაკომენტარეთ უარყოფითი პასუხის გაგზავნა.
					// await SendMessageAsync(senderId, "თქვენ უკვე მისეცით ხმა ბოლო წუთებში. გთხოვთ, მოიცადოთ.");
					result.SetError("Vote denied due to rate limit.");
					return result;


				}
			}
			else
			{
				result.SetError("UnRecognized Message.");
				return result;
			}
		}

		private async Task<OperationResult> ProcessPostbackAsync(string senderId, string recipientId, string msgId, string userName, string payload)
		{
			var result = new OperationResult(true);
			var voteName = payload.Split('_').Last();
			voteName = payload.Split(':').First();

            // 1. Check if the vote name corresponds to a registered client
            var client = _registeredClients.FirstOrDefault(c => c.clientName.Equals(voteName, StringComparison.OrdinalIgnoreCase));
			if (client == null)
			{
				_logger.LogWarningWithCaller($"Postback received for unknown client: {voteName}");
				result.SetError($"Unknown client: {voteName}");
				return result;
			}

			// 2. Try to acquire the lock (Rate Limiting per user)
			var lockKey = $"vote:{senderId}";
			var success = await _cacheService.AcquireLockAsync(lockKey, TimeSpan.FromMinutes(_voteMinuteRange));

			if (success)
			{
				var loggedInDB = await LogVoteRequestAsync(senderId, recipientId, msgId, userName, client.clientName, payload);

				
				if (loggedInDB.Result)
				{
                    var nextVoteTime = ((DateTime)loggedInDB.Results.GetType().GetProperty("Timestamp").GetValue(loggedInDB.Results)).AddMinutes(_voteMinuteRange);
                    // 4. Send Confirmation (Rate-limited, to prevent blocking)
                    var backMsg = $"თქვენი ხმა {client.clientName}-სთვის მიღებულია! მადლობა. ჩვენ კვლავ მივიღებთ თქვენს ხმას {_voteMinuteRange} წუთის მერე, {nextVoteTime} -დან";
					//var backMsg = $"Thank you! you voted for {client.clientName}! You can vote again in {_voteMinuteRange} minutes from now! After {nextVoteTime}";
					result = await SendMessageAsync(senderId, userName, backMsg);
					if (result.Result)
					{
						// Log API Call
						//_rateLimitingService.LogApiCall();
						// 5. Signal the server (If SignalRClient dependency is available and needed)
						// await _signalRClient.SendVoteUpdateAsync(client.Name, userName); 
						_logger.LogInformationWithCaller($"Vote from {senderId} for {client.clientName} recorded. Confirmation sent (if limit allows).");
						return result;
					}
					else
					{
						// TODO Delete from DataBase
						result.SetError("Failed to send confirmation message.");
						return result;
						
					}
					
				}
				else { 
					// TODO DO NOthing
					result.SetError("Failed to log vote in database.");
					return result;
				}

			}
			else
			{
				_logger.LogWarningWithCaller($"Vote from {senderId} for {voteName} NOT registered due to rate limit ({_voteMinuteRange} min). Confirmation skipped as per request.");

				// ❌ მოთხოვნისამებრ: დავაკომენტარეთ უარყოფითი პასუხის გაგზავნა.
				
				bool sendNotAcceptedVoteBackInfo = await _varsKeeper.GetValueAsync<bool>("fb_NotAcceptedVoteBackInfo");
				if (sendNotAcceptedVoteBackInfo)
					await SendMessageAsync(senderId, userName, $"თქვენი ბოლო ხმის მიცემიდან არ გასულა {_voteMinuteRange} წუთი.");
				//await SendMessageAsync(senderId, userName, $"It's been less than {_voteMinuteRange} minutes since your last vote. Please try again later.");

				result.SetError("Vote denied due to rate limit.");
				return result;
			}

			
		}


		private string getCandidateName(string VoteName)
		{
			var result = _registeredClients.Where(x=> x.clientName == VoteName).FirstOrDefault().phone;
			return result;
		}


		private async Task<OperationResult> LogVoteRequestAsync(string senderId, string recipientId, string MSGId, string userName, string voteName, string message)
		{
			var result = new OperationResult(false);

			var newVote = new Vote
			{
				Timestamp = DateTime.Now,
				Id = $"{senderId}.{DateTime.Now.ToString("yyyyMMddHHmmssfff")}",
				MSGId = MSGId,
				MSGRecipient = recipientId,
				UserName = userName,
				UserId = senderId,
				CandidateName = voteName,
				CandidatePhone = string.IsNullOrEmpty(voteName) ? "" : getCandidateName(voteName),
				Message = message
			};

			try
			{
				await _dbContext.FaceBookVotes.AddAsync(newVote);
				await _dbContext.SaveChangesAsync();
				result.SetSuccess();
				result.Results = new { newVote.Id, newVote.Timestamp};
			}
			catch (Exception ex)
			{
				result.SetError($"{ex}");				
			}
			return result;
		
		}



		/// <summary>
		/// Sends a text message to a specific user via the Facebook Messenger Send API.
		/// </summary>
		/// <param name="recipientId">The Facebook PSID (Page-Scoped User ID) of the recipient.</param>
		/// <param name="messageText">The text content of the message to send.</param>
		private async Task<OperationResult> SendMessageAsync(string recipientId, string senderName, string messageText)
		{
			var res = new OperationResult(true);

			// [RATE LIMIT CHECK] Check Rate Limit BEFORE making the API call (POST request)
			if (await _rateLimitingService.IsRateLimitExceeded("SendAPI:Text"))
			{
				_logger.LogWarningWithCaller($"[RATE LIMIT BLOCKED] Cannot send text to {recipientId}. Limit exceeded.");
				res.SetError($"[RATE LIMIT BLOCKED] Cannot send text to {recipientId}. Limit exceeded.");
				return res;
			}

			// 1. Construct the API Endpoint URL
			// We use the configured _pageAccessToken for authorization.
			var requestUrl = $"https://graph.facebook.com/v18.0/me/messages?access_token={_pageAccessToken}";

			// 2. Build the JSON Payload
			var jsonPayload = new
			{
				recipient = new { id = recipientId },
				message = new { text = messageText }
			};

			var content = new StringContent(
				JsonConvert.SerializeObject(jsonPayload),
				Encoding.UTF8,
				"application/json"
			);

			try
			{
				// 3. Log API Call (If your IRateLimitingService tracks individual calls)
				// If you track per-call, you may log it here. However, the check should be done 
				// BEFORE this method is called (e.g., in SendRateLimitedMessageAsync).

				// Log for visibility
				_logger.LogInformationWithCaller($"Attempting to send message to {recipientId}.");

				// 4. Send the HTTP Request
				var response = await _httpClient.PostAsync(requestUrl, content);

				// 5. Handle the Response
				if (response.IsSuccessStatusCode)
				{
					_logger.LogInformationWithCaller($"Message successfully sent to {recipientId}.");
					res.SetSuccess();
				}
				else
				{
					var responseContent = await response.Content.ReadAsStringAsync();
					_logger.LogErrorWithCaller($"Failed to send message to {recipientId}. Status: {response.StatusCode}. Response: {responseContent}");
					res.SetError($"Failed to send message to {recipientId}. Status: {response.StatusCode}. Response: {responseContent}");

					// Facebook may return 400 or 403 on rate limit, which should have been caught earlier.
					// If it happens here, it suggests the rate limit check was bypassed or failed.
				}
				return res;
			}
			catch (HttpRequestException ex)
			{
				_logger.LogErrorWithCaller($"HTTP Request error while sending message to {recipientId}. {ex}");
				res.SetError($"HTTP Request error while sending message to {{recipientId}}. {ex}");
			}
			catch (Exception ex)
			{
				_logger.LogErrorWithCaller($"General error in SendMessageAsync for {recipientId}. {ex}");
				res.SetError($"General error in SendMessageAsync for {recipientId}.{ex}");
			}
			return res;
		}


		/// <summary>
		/// ასრულებს API POST ზარს Facebook-ზე. (გამოიყენება შიგნით)
		/// </summary>
		private async Task<OperationResult> SendMessagePayLoadSafeAsync(string recipientId, object jsonPayload)
		{
			var res = new OperationResult(true);
			try
			{
				var content = new StringContent(
					JsonConvert.SerializeObject(jsonPayload),
					System.Text.Encoding.UTF8,
					"application/json"
				);

				var requestUri = $"https://graph.facebook.com/v18.0/me/messages?access_token={_pageAccessToken}";
				var response = await _httpClient.PostAsync(requestUri, content);

				if (response.IsSuccessStatusCode)
				{
					return res;
				}

				var responseContent = await response.Content.ReadAsStringAsync();
				_logger.LogErrorWithCaller($"Failed to send message to {recipientId}. Status: {response.StatusCode}. Response: {responseContent}");
				res.SetError($"Failed to send message to {recipientId}. Status: {response.StatusCode}. Response: {responseContent}");
				
			}
			catch (HttpRequestException ex)
			{
				_logger.LogErrorWithCaller($"HTTP Request error while sending message to {recipientId}. {ex}");
				res.SetError($"HTTP Request error while sending message to {recipientId}. {ex}");
			}
			catch (Exception ex)
			{
				_logger.LogErrorWithCaller($"Error sending message to {recipientId}: {ex.Message} {ex}");
				res.SetError($"Error sending message to {recipientId}: {ex.Message}");
				
			}
			return res;
		}

		/// <summary>
		/// Sends a carousel/generic template message with candidate images and voting buttons.
		/// </summary>
		/// <param name="recipientId">The Facebook PSID (Page-Scoped User ID) of the recipient.</param>
		/// <param name="imageUrls">List of image URLs for each gallery element.</param>
		/// <param name="names">List of candidate names (used for postback payload and button title).</param>
		private async Task<OperationResult> SendImageGalleryAsyncWithButtons(string senderId, string recipientId, string messageId, string userName, List<string> imageUrls, List<string> names)
		{
			var result = new OperationResult(true);




			var lockKeyForEnableVote = $"NeedForVote:{senderId}";
			var success = await _cacheService.AcquireLockAsync(lockKeyForEnableVote, TimeSpan.FromMinutes(_voteMinuteRange));

            // 🛑 ლიმიტის შემოწმება - კრიტიკული ნაბიჯი!
            // [RATE LIMIT CHECK] Check Rate Limit BEFORE making the API call (POST request)
            if (!success )
			{
				_logger.LogWarningWithCaller($"[TIME LIMIT BLOCKED] Cannot send gallery to {senderId}. TIME Limit exceeded.");
				result.SetError($"[TIME LIMIT BLOCKED] Cannot send gallery to {senderId}. TIME Limit exceeded.");
				return result;
			}


			if (await _rateLimitingService.IsRateLimitExceeded("SendAPI:Gallery"))
			{
				_logger.LogWarningWithCaller($"[RATE LIMIT BLOCKED] Cannot send gallery to {senderId}. Limit exceeded.");
				result.SetError($"[RATE LIMIT BLOCKED] Cannot send gallery to {senderId}. Limit exceeded.");
				return result;

			}

			// 1. Construct the API Endpoint URL
			//var requestUrl = $"https://graph.facebook.com/v18.0/me/messages?access_token={_pageAccessToken}";

			// 2. Build the Carousel Elements
			var elements = new List<object>();

			// Ensure we don't exceed Facebook's 10 element limit or mismatch list counts
			var maxElements = Math.Min(imageUrls.Count, names.Count);

			for (int i = 0; i < maxElements; i++)
			{
				var name = names[i];
				var imageUrl = imageUrls[i];
                
				if (!await IsImageUrlValidAsync(imageUrl))
                {
                    _logger.LogWarningWithCaller($"Invalid image URL detected, fallback applied: {imageUrl}");
                    imageUrl = _defaultCandidateImageUrl;
                }

                elements.Add(new
				{
					// Note: Title is optional in the Generic Template, but recommended for clarity.
					title = "I Vote for", //name,
					image_url = imageUrl,
					buttons = new[]
					{
					new
					{
						type = "postback",
						title = $"{name} 🟢 👍", // Georgian: Your vote for {name}
                        payload = $"{name}:YES" // Payload is the candidate name, used by the worker
                    }
//					,
//                    new
//                    {
//                        type = "postback",
//                        title = $"{name} 🔴 👎", // Georgian: Your vote for {name}
//                        payload = $"{name}:NO" // Payload is the candidate name, used by the worker
//                    }
                }
				});
			}

			// 3. Build the full JSON Payload (Generic Template)
			var jsonPayload = new
			{
				recipient = new { id = senderId },
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

			var loggedInDB = await LogVoteRequestAsync(senderId, recipientId, messageId, userName, "", _voteStartFlag); 

			if (loggedInDB.Result)
			{
				result = await SendMessagePayLoadSafeAsync(senderId, jsonPayload);

				if (result.Result)
				{
					// 5. Signal the server (If SignalRClient dependency is available and needed)
					// await _signalRClient.SendVoteUpdateAsync(client.Name, userName); 
					_logger.LogInformationWithCaller($"Request for Candidats from {recipientId} for {userName} recorded. Candidates Gallery sent (if limit allows).");
					// 📈 API ზარის დაფიქსირება
					//_rateLimitingService.LogApiCall();
				}
				else
				{
					// ❌ თუ გაგზავნა ვერ მოხერხდა, შედეგი გადმოვცემთ ზემოთ
					// TODO Delete from DataBase
				}
			}
			else
			{
				result.SetError("Failed to log vote request in database.");
			}

			return result;

		}

		public string? ExtractMessagePostbackPayLoad(JsonObject payload)
		{ 		
			try

			{
				var entry = payload["entry"]?.AsArray()?.FirstOrDefault()?.AsObject();
				var messaging = entry?["messaging"]?.AsArray()?.FirstOrDefault()?.AsObject();
				var postback = messaging?["postback"]?.AsObject();
				return postback?["payload"]?.GetValue<string>();

			}
			catch
			{
				return null;
			}
		}

		public string? ExtractMessageText(JsonObject payload)
		{
			try

			{
				var entry = payload["entry"]?.AsArray()?.FirstOrDefault()?.AsObject();
				var messaging = entry?["messaging"]?.AsArray()?.FirstOrDefault()?.AsObject();
				var text = messaging?["message"]?.AsObject();
				return text?["text"]?.GetValue<string>();

			}
			catch
			{
				return null;
			}
		}

		public string? ExtractMessageSenderOrRecipientId(JsonObject payload, string type ="sender")
		{
			try
			{
				var entry = payload["entry"]?.AsArray()?.FirstOrDefault()?.AsObject();
				var messaging = entry?["messaging"]?.AsArray()?.FirstOrDefault()?.AsObject();
				var sender = messaging?[type]?.AsObject();
				return sender?["id"]?.GetValue<string>();
			}
			catch
			{
				return null;
			}
		}
		public string? ExtractMessageId(JsonObject payload, string? messageType = null)
		{
			try
			{
				var entry = payload["entry"]?.AsArray()?.FirstOrDefault()?.AsObject();
				var messaging = entry?["messaging"]?.AsArray()?.FirstOrDefault()?.AsObject();
				if (messageType != null && messageType == "postback")
				{
					var postback = messaging?["postback"]?.AsObject();
					return postback?["mid"]?.GetValue<string>();
				}
				else
				{
					var message = messaging?["message"]?.AsObject();
					return message?["mid"]?.GetValue<string>();
				}
			}
			catch
			{
				return null;
			}
		}
		public string? ExtractMessageType(JsonObject payload)
		{
			// Navigate to the core messaging event object
			var messagingEvent = payload?["entry"]?.AsArray().FirstOrDefault()?
										.AsObject()?["messaging"]?.AsArray().FirstOrDefault()?
										.AsObject();

			if (messagingEvent == null)
			{
				return null;
			}

			// Check for the presence of known event type keys
			if (messagingEvent.ContainsKey("postback"))
			{
				return "postback";
			}

			if (messagingEvent.ContainsKey("message"))
			{
				return "message";
			}

			if (messagingEvent.ContainsKey("reaction"))
			{
				return "reaction";
			}

			if (messagingEvent.ContainsKey("read"))
			{
				return "read";
			}

			// Return null or "unknown" if no known type is found
			return null;
		}

		/// <summary>
		/// Retrieves the user's name, prioritizing the Redis cache to minimize Facebook Graph API calls.
		/// </summary>
		private async Task<string> GetUserNameAsync(string? userId, string? recipientId)
		{
			if (string.IsNullOrEmpty(userId)) return "უცნობი მომხმარებელი"; // Georgian for "Unknown User"

			//var cacheKey = $"userName:{userId}";
			var cacheKey = $"FB:Recipient:{recipientId}:User:{userId}:Name";


			// 1. Check Cache
			var userName = await _cacheService.GetAsync<string>(cacheKey);

			if (!string.IsNullOrEmpty(userName))
			{
				// Cache Hit! Skip the slow external API call.
				return userName;
			}

			try
			{

				// [RATE LIMIT CHECK] Check Rate Limit BEFORE making the API call (GET request)
				if (await _rateLimitingService.IsRateLimitExceeded("GraphAPI:GetUserName"))
				{
					_logger.LogWarningWithCaller($"[RATE LIMIT BLOCKED] Cannot fetch user name for {userId}. Limit exceeded.");
					return "უცნობი მომხმარებელი"; // Fail safe: return unknown name
				}

				// 2. Cache Miss: Perform the slow Facebook Graph API call
				// NOTE: Ensure this call is checked by IRateLimitingService if you have a separate limit for GET requests.
				var requestUrl = $"https://graph.facebook.com/v18.0/{userId}?fields=first_name,last_name&access_token={_pageAccessToken}";
				var response = await _httpClient.GetAsync(requestUrl);

				if (response.IsSuccessStatusCode)
				{
					var responseContent = await response.Content.ReadAsStringAsync();
					var userInfo = JObject.Parse(responseContent);

					var firstName = userInfo["first_name"]?.Value<string>();
					var lastName = userInfo["last_name"]?.Value<string>();

					// Construct full name
					userName = $"{firstName} {lastName}".Trim();
					if (string.IsNullOrEmpty(userName)) userName = "უცნობი მომხმარებელი";

					// 3. Store result in Redis for future requests (Cache for 7 days)
					await _cacheService.SetAsync(cacheKey, userName, TimeSpan.FromDays(7));

					return userName;
				}
				else
				{
					_logger.LogErrorWithCaller($"Failed to fetch user name for {userId}. Status: {response.StatusCode}, ResquestUri {response.RequestMessage?.RequestUri?.AbsoluteUri}");
					return "უცნობი მომხმარებელი";
					
				}
			}
			catch (Exception ex)
			{
				_logger.LogErrorWithCaller($"Error calling Graph API for user {userId}. {ex}");
				return "უცნობი მომხმარებელი"; // Fallback name
			}
		}


        private async Task<bool> IsImageUrlValidAsync(string url)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Head, url);
                using var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    return false;

                if (!response.Content.Headers.ContentType?.MediaType?.StartsWith("image") ?? true)
                    return false;

                if (response.Content.Headers.ContentLength > 8_000_000)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

    }

}
