using System;
using GameController.FBService.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace GameController.FBService.Services
{
	public class WebhookProcessorService : IWebhookProcessorService
	{
		private readonly ILogger<WebhookProcessorService> _logger;
		private readonly ApplicationDbContext _dbContext;
		private readonly ICacheService _cacheService; // NEW: Redis service
		private readonly IRateLimitingService _rateLimitingService; // Existing service, now Redis-backed
		private readonly HttpClient _httpClient; // HttpClient from the original controller

		// Configuration fields moved from the controller
		private readonly List<ClientConfiguration> _registeredClients;
		private readonly string _voteStartFlag;
		private readonly string _pageAccessToken;
		private readonly string _imageFolderPath;
		private readonly int _voteMinuteRange;

		public WebhookProcessorService(ILogger<WebhookProcessorService> logger,
			ApplicationDbContext dbContext,
			ICacheService cacheService,
			IRateLimitingService rateLimitingService,
			IConfiguration configuration,
			IHttpClientFactory httpClientFactory
			)
		{
			_logger = logger;
			_dbContext = dbContext;
			_cacheService = cacheService;
			_rateLimitingService = rateLimitingService;

			// Initialization of configuration fields (moved from controller)
			_registeredClients = configuration.GetSection("RegisteredClients").Get<List<ClientConfiguration>>() ?? new List<ClientConfiguration>();
			_voteStartFlag = configuration.GetValue<string>("voteStartFlag") ?? "";
			_imageFolderPath = configuration.GetValue<string>("imageFolderPath") ?? "";
			_pageAccessToken = configuration.GetValue<string>("pageAccessToken") ?? "";
			_voteMinuteRange = configuration.GetValue<int>("voteMinuteRange", 5);

			_httpClient = httpClientFactory.CreateClient();


		}

		// ----------------------------------------------------
		// CORE WEBHOOK PROCESSING METHOD
		// ----------------------------------------------------
		public async Task ProcessWebhookMessageAsync(string rawPayload)
		{
			JObject payload;
			try
			{
				payload = JObject.Parse(rawPayload);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Could not parse JSON payload. Data: {rawPayload}");
				return;
			}

			var messagingEvents = payload["entry"]?
								.SelectMany(e => e["messaging"] ?? new JArray())
								.ToArray();

			if (messagingEvents == null || messagingEvents.Length == 0)
			{
				return;
			}

			foreach (var messageObject in messagingEvents)
			{
				var senderId = messageObject?["sender"]?["id"]?.Value<string>();
				var receivedText = messageObject?["message"]?["text"]?.Value<string>();
				var postbackPayload = messageObject?["postback"]?["payload"]?.Value<string>();

				if (string.IsNullOrEmpty(senderId)) continue;

				// Lock-ის პარამეტრები (გამოიყენება მხოლოდ ხმის მიცემისას)
				var lockKey = $"voteLock:{senderId}";
				var lockDuration = TimeSpan.FromMinutes(_voteMinuteRange);

				try
				{
					var senderName = await GetUserNameAsync(senderId);

					// A. START VOTE LOGIC (Text Message "A")
					if (receivedText?.ToLower() == _voteStartFlag)
					{
						// აქ Lock-ის შემოწმება არ გვჭირდება, რადგან ეს მხოლოდ გალერეის მოთხოვნაა.

						var imageUrls = _registeredClients.Select(x => _imageFolderPath + x.image).ToList();
						var names = _registeredClients.Select(x => x.clientName).ToList();

						await SendRateLimitedImageGalleryAsyncWithButtons(senderId, imageUrls, names);
						_logger.LogInformation($"FbUser: {senderId} Requested vote gallery.");

						// DB-ში ჩაწერა, რომ მოთხოვნა გაკეთდა (სურვილისამებრ)
						await LogVoteRequestAsync(senderId, senderName, receivedText);
					}
					// B. VOTE LOGIC (Postback from Button Click)
					else if (!string.IsNullOrEmpty(postbackPayload))
					{
						// 1. Lock-ის შემოწმება: ვცდილობთ Lock-ის აღებას.
						// თუ Lock-ი უკვე არსებობს (ანუ ხმა ახლახან დაფიქსირდა), ეს მეთოდი დააბრუნებს false-ს.
						var lockAcquiredForVote = await _cacheService.AcquireLockAsync(lockKey, lockDuration);

						if (!lockAcquiredForVote)
						{
							// Lock-ი ვერ აღდგა, ანუ ხმა უკვე მიცემულია ამ პერიოდში.
							_logger.LogInformation($"Vote denied for {senderId}. Lock held (within {_voteMinuteRange} min interval).");
							await SendRateLimitedMessageAsync(senderId, $"თქვენ უკვე მიეცით ხმა ბოლო {_voteMinuteRange} წუთის განმავლობაში. გთხოვთ, დაელოდოთ.");
							continue;
						}

						// --- Lock Acquired SUCCESSFULLY for the VOTE ---

						var names = _registeredClients.Select(x => x.clientName).ToList();
						var isValidCandidate = names.FirstOrDefault(c => c == postbackPayload);

						if (isValidCandidate != null)
						{
							_logger.LogInformation($"FbUser: ({senderId}) {senderName} voted for: {isValidCandidate}");

							// ხმის ჩაწერა მონაცემთა ბაზაში და SignalR-ის გაგზავნა
							var newVoteId = await LogAndProcessUserVoteAsync(senderId, senderName, isValidCandidate, postbackPayload);

							// პასუხის გაგზავნა მომხმარებლისთვის
							await SendRateLimitedMessageAsync(senderId, $"თქვენი ხმა წარმატებით ჩაიწერა! თქვენ მიეცით ხმა {isValidCandidate}-ს.");
						}
						else
						{
							// თუ Lock-ი ავიღეთ, მაგრამ Payload არასწორია, Lock-ი მაინც დარჩება (Cool-down)
							_logger.LogWarning($"Invalid candidate vote payload received for {senderId}: {postbackPayload}");
							await SendRateLimitedMessageAsync(senderId, $"თქვენი არჩევანი არასწორია.");
						}
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, $"Critical error during vote processing for {senderId}.");
					// Lock-ი ავტომატურად გაუქმდება ვადის გასვლის შემდეგ.
				}
			}
		}


		private async Task LogVoteRequestAsync(string userId, string userName, string message)
		{
			var newVote = new Vote
			{
				Timestamp = DateTime.Now,
				Id = $"{userId}.{DateTime.Now.ToString("yyyyMMddHHmmssfff")}",
				UserName = userName,
				UserId = userId,
				CandidateName = string.Empty,
				Message = message
			};
			await _dbContext.FaceBookVotes.AddAsync(newVote);
			await _dbContext.SaveChangesAsync();
		}

		private async Task<string> LogAndProcessUserVoteAsync(string userId, string userName, string candidateName, string message)
		{
			var timestamp = DateTime.Now;
			var newVote = new Vote
			{
				Timestamp = timestamp,
				Id = $"{userId}.{timestamp.ToString("yyyyMMddHHmmssfff")}",
				UserName = userName,
				UserId = userId,
				CandidateName = candidateName,
				Message = message
			};
			await _dbContext.FaceBookVotes.AddAsync(newVote);
			await _dbContext.SaveChangesAsync();

			// 3. SignalR-ის მეშვეობით სხვა აპლიკაციების ინფორმირება
			//await _signalRClient.SendVoteUpdate(userName, candidateName);

			return newVote.Id;
		}


		public async Task ProcessWebhookMessageAsync_(string rawPayload)
		{
			// 1. Parse the Raw Payload
			JObject payload;
			try
			{
				payload = JObject.Parse(rawPayload);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Could not parse JSON payload. Data: {rawPayload}");
				// Log and discard, or send to a Dead-Letter Queue (DLQ).
				return;
			}

			var messaging = payload["entry"]?.SelectMany(e => e["messaging"] ?? new JArray())?.ToArray();

			if (messaging == null || messaging.Length == 0)
			{
				// This payload was likely a change in feed/reaction, which we are not processing here.
				return;
			}

			foreach (var messageObject in messaging)
			{
				// Detailed extraction using JToken
				var senderId = messageObject?["sender"]?["id"]?.Value<string>();
				var receivedMessage = messageObject?["message"]?["text"]?.Value<string>();
				var timeStamp = messageObject?["timestamp"]?.Value<long>();
				var postBack = messageObject?["postback"]?["payload"]?.Value<string>();

				if (string.IsNullOrEmpty(senderId) || timeStamp == null)
				{
					_logger.LogWarning("Missing Sender ID or Timestamp in message object.");
					continue;
				}

				string candidateVote = receivedMessage?.ToLower() == _voteStartFlag ? string.Empty : postBack;

				// --- CONCURRENCY & VOTE LIMIT CHECK (USING REDIS LOCK) ---
				var lockKey = $"voteLock:{senderId}";
				var lockDuration = TimeSpan.FromMinutes(_voteMinuteRange);

				// Attempt to acquire the lock atomically.
				var lockAcquired = await _cacheService.AcquireLockAsync(lockKey, lockDuration);

				if (!lockAcquired)
				{
					_logger.LogInformation($"Vote denied for {senderId}. Lock held (within {_voteMinuteRange} min interval).");

					// We can attempt to send a polite rate-limit message back (after a rate limit check)
					// (Ensure SendMessageAsync respects the Facebook API rate limits!)
					await SendRateLimitedMessageAsync(senderId, $"თქვენ არ შეგიძლიათ ხმის მიცემა ამ წუთას. გთხოვთ სცადოთ ისევ {DateTime.Now.Add(lockDuration).ToString("HH:mm")}-ის შემდეგ.");
					continue;
				}

				// Lock acquired successfully (This means the user hasn't voted in the last _voteMinuteRange, OR
				// another worker hasn't finished processing their vote yet).

				try
				{
					var senderName = await GetUserNameAsync(senderId); // Uses Redis cache internally

					if (receivedMessage?.ToLower() == _voteStartFlag)
					{
						// 3a. User requested the start of voting
						var imageUrls = _registeredClients.Select(x => _imageFolderPath + x.image).ToList();
						var names = _registeredClients.Select(x => x.clientName).ToList();

						await SendRateLimitedImageGalleryAsyncWithButtons(senderId, imageUrls, names);
						_logger.LogInformation($"FbUser: {senderId} Requested enable vote, gallery sent.");

						// Save a 'start' entry to the DB (This is just for tracking the request, not the vote itself)
						var newVote = new Vote
						{
							Timestamp = DateTime.Now,
							Id = $"{senderId}.{DateTime.Now.ToString("yyyyMMddHHmmssfff")}",
							UserName = senderName,
							UserId = senderId,
							CandidateName = string.Empty,
							Message = receivedMessage ?? string.Empty
						};
						await _dbContext.FaceBookVotes.AddAsync(newVote);
						await _dbContext.SaveChangesAsync();
					}
					else if (!string.IsNullOrEmpty(candidateVote))
					{
						// 3b. User sent a postback/vote
						var names = _registeredClients.Select(x => x.clientName).ToList();
						var isValidAnswer = names.FirstOrDefault(c => c == candidateVote);

						if (isValidAnswer != null)
						{
							_logger.LogInformation($"FbUser: ({senderId}) {senderName} voted for: {candidateVote}");

							var timestamp = DateTime.Now;
							var newVote = new Vote
							{
								Timestamp = timestamp,
								Id = $"{senderId}.{timestamp.ToString("yyyyMMddHHmmssfff")}",
								UserName = senderName,
								UserId = senderId,
								CandidateName = candidateVote,
								Message = postBack ?? receivedMessage ?? string.Empty
							};
							await _dbContext.FaceBookVotes.AddAsync(newVote);
							await _dbContext.SaveChangesAsync();

							await SendRateLimitedMessageAsync(senderId, $"თქვენი შეტყობინება მიღებულია! თქვენ მიეცით ხმა {candidateVote}ს, რეგისტრაციის ID: {newVote.Id}");
						}
					}

					// The lock will naturally expire based on lockDuration (_voteMinuteRange). 
					// No need to release it explicitly here if the vote was successful, as the 
					// lock expiration manages the cool-down period.
				}
				catch (DbUpdateException dbEx)
				{
					// Handle DB errors (e.g., connection issues)
					_logger.LogError(dbEx, $"Database error while saving vote for {senderId}.");
					// Important: Since the vote failed, we should release the lock if it was a failure 
					// unrelated to the time limit. (In this setup, we rely on the DB being reliable).
				}
				catch (Exception ex)
				{
					// General processing error
					_logger.LogError(ex, $"Critical error during vote processing for {senderId}.");
				}
			}
		}

		private async Task SendRateLimitedMessageAsync(string recipientId, string messageText)
		{
			if (await _rateLimitingService.IsRateLimitExceeded("messages"))
			{
				_logger.LogWarning($"Facebook message API limit exceeded. Message to {recipientId} deferred or dropped.");
				// OPTION: Send to a delayed queue for a retry.
				return;
			}
			await SendMessageAsync(recipientId, messageText); // The original implementation
		}

		private async Task SendRateLimitedImageGalleryAsyncWithButtons(string recipientId, List<string> imageUrls, List<string> names)
		{
			if (await _rateLimitingService.IsRateLimitExceeded("messages"))
			{
				_logger.LogWarning($"Facebook message API limit exceeded. Gallery to {recipientId} deferred or dropped.");
				// OPTION: Send to a delayed queue for a retry.
				return;
			}
			await SendImageGalleryAsyncWithButtons(recipientId, imageUrls, names); // The original implementation
		}

		/// <summary>
		/// Sends a text message to a specific user via the Facebook Messenger Send API.
		/// </summary>
		/// <param name="recipientId">The Facebook PSID (Page-Scoped User ID) of the recipient.</param>
		/// <param name="messageText">The text content of the message to send.</param>
		private async Task SendMessageAsync(string recipientId, string messageText)
		{
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
				_logger.LogInformation($"Attempting to send message to {recipientId}.");

				// 4. Send the HTTP Request
				var response = await _httpClient.PostAsync(requestUrl, content);

				// 5. Handle the Response
				if (response.IsSuccessStatusCode)
				{
					_logger.LogInformation($"Message successfully sent to {recipientId}.");
				}
				else
				{
					var responseContent = await response.Content.ReadAsStringAsync();
					_logger.LogError($"Failed to send message to {recipientId}. Status: {response.StatusCode}. Response: {responseContent}");

					// Facebook may return 400 or 403 on rate limit, which should have been caught earlier.
					// If it happens here, it suggests the rate limit check was bypassed or failed.
				}
			}
			catch (HttpRequestException ex)
			{
				_logger.LogError(ex, $"HTTP Request error while sending message to {recipientId}.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"General error in SendMessageAsync for {recipientId}.");
			}
		}

		/// <summary>
		/// Sends a carousel/generic template message with candidate images and voting buttons.
		/// </summary>
		/// <param name="recipientId">The Facebook PSID (Page-Scoped User ID) of the recipient.</param>
		/// <param name="imageUrls">List of image URLs for each gallery element.</param>
		/// <param name="names">List of candidate names (used for postback payload and button title).</param>
		private async Task SendImageGalleryAsyncWithButtons(string recipientId, List<string> imageUrls, List<string> names)
		{
			// 1. Construct the API Endpoint URL
			var requestUrl = $"https://graph.facebook.com/v18.0/me/messages?access_token={_pageAccessToken}";

			// 2. Build the Carousel Elements
			var elements = new List<object>();

			// Ensure we don't exceed Facebook's 10 element limit or mismatch list counts
			var maxElements = Math.Min(imageUrls.Count, names.Count);

			for (int i = 0; i < maxElements; i++)
			{
				var name = names[i];
				var imageUrl = imageUrls[i];

				elements.Add(new
				{
					// Note: Title is optional in the Generic Template, but recommended for clarity.
					title = name,
					image_url = imageUrl,
					buttons = new[]
					{
					new
					{
						type = "postback",
						title = $"შენი ხმა {name}-ს", // Georgian: Your vote for {name}
                        payload = $"{name}" // Payload is the candidate name, used by the worker
                    }
				}
				});
			}

			// 3. Build the full JSON Payload (Generic Template)
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
				JsonConvert.SerializeObject(jsonPayload),
				Encoding.UTF8,
				"application/json"
			);

			try
			{
				_logger.LogInformation($"Attempting to send image gallery to {recipientId} with {elements.Count} options.");

				// 4. Send the HTTP Request
				var response = await _httpClient.PostAsync(requestUrl, content);

				// 5. Handle the Response
				if (response.IsSuccessStatusCode)
				{
					_logger.LogInformation($"Image gallery successfully sent to {recipientId}.");
				}
				else
				{
					var responseContent = await response.Content.ReadAsStringAsync();
					_logger.LogError($"Failed to send gallery to {recipientId}. Status: {response.StatusCode}. Response: {responseContent}");
				}
			}
			catch (HttpRequestException ex)
			{
				_logger.LogError(ex, $"HTTP Request error while sending gallery to {recipientId}.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"General error in SendImageGalleryAsyncWithButtons for {recipientId}.");
			}
		}

		/// <summary>
		/// Retrieves the user's name, prioritizing the Redis cache to minimize Facebook Graph API calls.
		/// </summary>
		private async Task<string> GetUserNameAsync(string userId)
		{
			var cacheKey = $"userName:{userId}";

			// 1. Check Cache
			var userName = await _cacheService.GetAsync<string>(cacheKey);

			if (!string.IsNullOrEmpty(userName))
			{
				// Cache Hit! Skip the slow external API call.
				return userName;
			}

			try
			{
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

				_logger.LogError($"Failed to fetch user name for {userId}. Status: {response.StatusCode}");
				return "უცნობი მომხმარებელი";
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error calling Graph API for user {userId}.");
				return "უცნობი მომხმარებელი"; // Fallback name
			}
		}


	}

}

