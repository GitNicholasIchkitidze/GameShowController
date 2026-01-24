using Azure;
using GameController.FBService.AntiBotServices;
using GameController.FBService.Extensions;
using GameController.FBService.Heplers;

using GameController.FBService.Models;
using GameController.Shared.Models;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Azure.Core.HttpHeader;
using static StackExchange.Redis.Role;

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

        private readonly VotingOptions _voting;

        private readonly IAppMetrics _metrics;
		private readonly IDempotencyService _dempotencyService;

		// Configuration fields moved from the controller
		private readonly List<FBClientConfiguration> _registeredClients;
		private readonly string _voteStartFlag;
        private readonly string _voteSession;
        private readonly string _pageAccessToken;
		private readonly string _imageFolderPath;
        private readonly string _defaultCandidateImageUrl;
		private readonly string _defaultAskConfirmationImageUrl;
        private readonly int _voteMinuteRange;
		private readonly int _voteConfirmationDuration;
        private readonly bool _checkForClickers;
		private readonly bool _sendVoteConfirmationToBadUsers;
        private readonly bool _sendVoteConfirmationToAllUsers;
        private readonly IGlobalVarsKeeper _varsKeeper;

		private readonly IConfiguration _configuration;
		private readonly IClickerDetectionService _clickerDetection;

		private readonly IRedisTtlProvider _ttl;


        private readonly ICommonHelper _commonHelper;
        private readonly IPayloadHelper _payloadHelper;

        public WebhookProcessorService(ILogger<WebhookProcessorService> logger,
			ApplicationDbContext dbContext,
			ICacheService cacheService,
			IRateLimitingService rateLimitingService,
			IConfiguration configuration,
			IAppMetrics metrics,
			IHttpClientFactory httpClientFactory,
			IDempotencyService dempotencyService,
			IRedisTtlProvider ttl,
			IClickerDetectionService clickerDetection,
			IGlobalVarsKeeper varsKeeper,
            IPayloadHelper payloadHelper,
            IOptions<VotingOptions> voting,
            ICommonHelper commonHelper

            )
		{
			_logger = logger;
			_dbContext = dbContext;
			_cacheService = cacheService;
			_rateLimitingService = rateLimitingService;
			_dempotencyService = dempotencyService;
			_varsKeeper = varsKeeper;
			_metrics = metrics;
            _voting = voting.Value;

            // Initialization of configuration fields (moved from controller)
            _registeredClients = configuration.GetSection("RegisteredClients").Get<List<FBClientConfiguration>>() ?? new List<FBClientConfiguration>();
			_voteStartFlag = configuration.GetValue<string>("voteStartFlag") ?? "";
			_voteSession = configuration.GetValue<string>("VoteSessionPrefix") ?? "";
            _imageFolderPath = configuration.GetValue<string>("imageFolderPath") ?? "";
			_pageAccessToken = configuration.GetValue<string>("pageAccessToken") ?? "";
			_voteMinuteRange = configuration.GetValue<int>("voteMinuteRange", 5);
            _checkForClickers = configuration.GetValue<bool>("checkForClickers", false);
            _sendVoteConfirmationToBadUsers = configuration.GetValue<bool>("sendVoteConfirmationToBadUsers", false);
            _sendVoteConfirmationToAllUsers = configuration.GetValue<bool>("sendVoteConfirmationToAllUsers", false);
            _voteConfirmationDuration = configuration.GetValue<int>("voteConfirmationDuration", 120);
            _defaultCandidateImageUrl = configuration.GetValue<string>("defaultCandidateImageUrl","");
            _defaultAskConfirmationImageUrl = configuration.GetValue<string>("defaultAskConfirmationImageUrl", "");

            _httpClient = httpClientFactory.CreateClient();

			_configuration = configuration;
			_ttl = ttl;
			_voteMinuteRange = _ttl.VoteCooldown.Minutes;
			_clickerDetection = clickerDetection;
            _payloadHelper = payloadHelper;

            _commonHelper = commonHelper;

        }

		// ----------------------------------------------------
		// CORE WEBHOOK PROCESSING METHOD
		// ----------------------------------------------------


		



		public async Task<OperationResult> ProcessWebhookMessageAsync(string rawPayload)    // ADDED (2025-12):
		{
			// CHANGED (2025-12-29): All parsing/filtering/idempotency moved here from the controller.
			// Controller is "ingress-only" and returns 200 ASAP.
			var res = new OperationResult(true);

			if (string.IsNullOrWhiteSpace(rawPayload))
			{
				res.Message = "Empty payload ignored.";
				return res;
			}

			JsonObject? payload;
			try
			{
				payload = JsonNode.Parse(rawPayload)?.AsObject();
			}
			catch (Exception ex)
			{
				// Malformed JSON - treat as a processing failure (rare, but important to see in logs).
				res.SetError($"Invalid JSON payload: {ex.Message}");
				return res;
			}



			// 1) Fast global "listening" gate (Redis-backed). Kept here to keep webhook ACK fast.
			// NOTE: This is still a Redis read per message. If you need more throughput later,
			// we can cache this value in-memory for 250-1000ms.
			bool isListening = await _varsKeeper.GetValueAsync<bool>("fb_listening_active");
			if (!isListening)
			{
				res.SetError("Listening disabled. Ignored.");
				return res;
			}

			// 2) Determine type and apply cheap filters BEFORE any heavy work (DB/Graph/Send).
			var messageType = _payloadHelper. ExtractMessageType(payload);
			if (string.IsNullOrEmpty(messageType))
			{
				res.SetError("Message type missing. Ignored.");
				return res;
			}

			// Only allow: voteStart text message OR postback OR reaction (optional)
			if (messageType.Equals("message", StringComparison.OrdinalIgnoreCase))
			{
				var text =_payloadHelper.ExtractMessageText(payload);
				if (!string.Equals(text, _voteStartFlag, StringComparison.OrdinalIgnoreCase))
				{
					// Not a voteStart request -> ignore silently (not an error)
					res.SetError("Non-voteStart message ignored.");
					return res;
				}
			}
			else if (!messageType.Equals("postback", StringComparison.OrdinalIgnoreCase) &&
					 !messageType.Equals("reaction", StringComparison.OrdinalIgnoreCase))
			{
				res.SetError($"Unsupported messageType '{messageType}' ignored.");
				return res;
			}

			// 3) Idempotency check (moved from controller).
			var messageId = _payloadHelper.ExtractMessageId(payload, messageType);
			if (string.IsNullOrEmpty(messageId))
			{
				res.SetError("MessageId missing. Ignored.");
				return res;
			}

			if (await _dempotencyService.IsDuplicateAsync(messageId))
			{
				res.SetError($"Duplicate message ignored: {messageId}");
				return res;
			}

			// 4) Extract sender/recipient and basic validation
			var senderId = _payloadHelper.ExtractMessageSenderOrRecipientId(payload, "sender");
			var recipientId = _payloadHelper.ExtractMessageSenderOrRecipientId(payload, "recipient");

			
            if (!_commonHelper.IsThisMe(senderId))
            {
                res.SetError($"Halted by Debug purposes: 200");
                return res;

            }
			


            if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(recipientId))
			{
				res.SetError("Sender/Recipient missing. Ignored.");
				return res;
			}

			if (senderId == recipientId)
			{
				res.SetError("Self-sent message ignored.");
				return res;
			}

			// 5) Fetch userName ONLY when we actually process (can be expensive - GraphAPI).
			var gotName = await GetUserNameAsync(senderId, recipientId);
			var userName= gotName.Result ? gotName.Message : "UNKNOWN";

			
			// 6) Route to business logic
			switch (messageType.ToLowerInvariant())
			{
				case "message":
					{
						var messageText = _payloadHelper.ExtractMessageText(payload);
						if (!string.IsNullOrEmpty(messageText))
						{
							return await ProcessTextMessageAsync(senderId, recipientId, messageId, userName, messageText);
						}

						res.SetError("Empty text message ignored.");
						return res;
					}

				case "postback":
					{

						var postbackPayload = _payloadHelper.ExtractMessagePostbackPayLoad(payload);

						//if (!postbackPayload.StartsWith(_voteSession))
						//{
						//	return await ProcessTextMessageAsync(senderId, recipientId, messageId, userName, _voteStartFlag);
                        //}
						//else
						//{
                        //    postbackPayload = postbackPayload[_voteSession.Length..];
						//
                        //}



						if (!string.IsNullOrEmpty(postbackPayload))
						{
							var postbackPayloadSplitted = postbackPayload.Split('_');

                            if (postbackPayloadSplitted.Count() > 1)
							{
								DateTime.TryParse(postbackPayloadSplitted[1], out DateTime dateResult);
                                
								if (dateResult.AddSeconds(_voteConfirmationDuration) > DateTime.Now)
								{
                                    _logger.LogWarningWithCaller($"Vote CONFIRMATION TIME IS OK {dateResult.AddSeconds(_voteConfirmationDuration)} is more than  arrival {DateTime.Now}. ");
                                    postbackPayload = postbackPayloadSplitted[0] + ":" + postbackPayloadSplitted[2] + "Confirmed";

                                }
								else
								{
                                    _logger.LogWarningWithCaller($"Vote CONFIRMATION TIME ELAPSED {dateResult.AddSeconds(_voteConfirmationDuration)} is less than  arrival {DateTime.Now}. ");
                                    res.SetError("Vote Confirmation Time elapsed.");
                                    return res;
                                }

                            }

							
                            return await ProcessPostbackAsync_TryCatchClicker_Alter(senderId, recipientId, messageId, userName, postbackPayload);
                        }

						res.SetError("Empty postback payload ignored.");
						return res;
					}

				case "reaction":
					return await HandleReactionEvent(payload);

				default:
					res.SetError($"Unhandled messageType '{messageType}' ignored.");
					return res;
			}
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


				//var lockKeyForVote = $"vote:{senderId}:{recipientId}";
				//var lockKeyForEnableVote = $"NeedForVote:{senderId}:{recipientId}";
				var lockKeyForVote = RedisKeys.FB.Native.VoteLock(recipientId, senderId);                 // ახალი დამატებული
				var lockKeyForEnableVote = RedisKeys.FB.Native.NeedForVoteLock(recipientId, senderId);   //

				//var isLockedKeyForVote = await _cacheService.GetAcquiredLockAsync(lockKeyForVote, TimeSpan.FromMinutes(_voteMinuteRange));
				var isLockedKeyForVote = await _cacheService.GetAcquiredLockAsync(lockKeyForVote, _ttl.VoteCooldown);
				//var isLockedKeyForEnableVote = await _cacheService.GetAcquiredLockAsync(lockKeyForEnableVote, TimeSpan.FromMinutes(_voteMinuteRange));
				var isLockedKeyForEnableVote = await _cacheService.GetAcquiredLockAsync(lockKeyForEnableVote, _ttl.VoteCooldown);
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

			var lockKey = RedisKeys.FB.Native.VoteLock(recipientId, senderId); // ახალი დამატებული

			
			var success = await _cacheService.AcquireLockAsync(lockKey, _ttl.VoteCooldown);
            _logger.LogWarningWithCaller($"VoteLock  {lockKey}  after AcquireLockAsync");

			if (success)
			{
				var decision = new ClickerDecision();

				if (_checkForClickers)
				{
					decision = await _clickerDetection.EvaluateAsync(recipientId, senderId, userName, DateTime.UtcNow); // ახალი დამატებული

					_metrics.IncRiskBandByCandidate(client.clientName, decision.RiskScore, 100, 160); // Blocked=RiskScore>=100
					_metrics.IncCandidateFlags(client.clientName, decision.Flags);

					//if (decision?.Flags != null)
					//{
					//	foreach (var f in decision.Flags)
					//		_metrics.IncCandidateFlag(client.clientName, f);
					//}

					if (decision?.IsSuspicious == true)
					{
						_metrics.IncSuspiciousByCandidate(client.clientName);
					}

                    // ShouldBlock თუ გინდა
                    //decision.ShouldBlock = true;



					//if (decision?.ShouldBlock == true  )
                    if (decision != null && !payload.EndsWith("Confirmed", StringComparison.Ordinal))
                    {
						_metrics.IncBlockedByCandidate(client.clientName);
						await LogClickerAccountAsync(senderId, recipientId, msgId, userName, client.clientName, payload, decision);

						if (_sendVoteConfirmationToBadUsers)
						{

							var names = new List<string>();
							//foreach (var candidat in _registeredClients)
							//{
							//	names.Add(candidat.clientName);
							//}

                            names.Add(voteName);
                            names.Add("-");
                            names.Add("-");

                            _logger.LogWarningWithCaller($"VoteLock  {lockKey} before ReleaseLockAsync");
                            await _cacheService.ReleaseLockAsync(lockKey);
                            var stillLocked = await _cacheService.GetAcquiredLockAsync(lockKey);
                            _logger.LogWarningWithCaller($"[DEBUG] After ReleaseLockAsync: lockKey={lockKey}, stillLocked={stillLocked}");


                            return await SendAskForVoteConfirmationAsyncWithButtons(senderId, new List<string> { _defaultAskConfirmationImageUrl }, names);


						}
					}
				}
				


				var loggedInDB = await LogVoteRequestAsync(senderId, recipientId, msgId, userName, client.clientName, payload, decision);

				
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
						//_logger.LogInformationWithCaller($"Vote from {senderId} for {client.clientName} recorded. Confirmation sent (if limit allows).");
						return result;
					}
					else
					{
						// TODO Delete from DataBase
						//result.SetError("Failed to send confirmation message.");
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

        private async Task<OperationResult> ProcessPostbackAsync_TryCatchClicker(string senderId, string recipientId, string msgId, string userName, string payload)
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

            var lockKey = RedisKeys.FB.Native.VoteLock(recipientId, senderId); 


            var success = await _cacheService.AcquireLockAsync(lockKey, _ttl.VoteCooldown);
            _logger.LogWarningWithCaller($"VoteLock  {lockKey}  after AcquireLockAsync");

            if (success)
            {
                var decision = new ClickerDecision();

                if (_checkForClickers)
                {
                    decision = await _clickerDetection.EvaluateAsync(recipientId, senderId, userName, DateTime.UtcNow); // ახალი დამატებული

					await _clickerDetection.ApplyClikerMetrics(client.clientName, decision);






                    if (decision?.ShouldBlock == true  )                    
                    {
                        
                        await LogClickerAccountAsync(senderId, recipientId, msgId, userName, client.clientName, payload, decision);

                        if (_sendVoteConfirmationToBadUsers)
                        {

                            var names = new List<string>();

                            names.Add(voteName);
                            names.Add("-");
                            names.Add("-");

                            //_logger.LogWarningWithCaller($"VoteLock  {lockKey} before ReleaseLockAsync");
                            await _cacheService.ReleaseLockAsync(lockKey);
                            var stillLocked = await _cacheService.GetAcquiredLockAsync(lockKey);
                            //_logger.LogWarningWithCaller($"[DEBUG] After ReleaseLockAsync: lockKey={lockKey}, stillLocked={stillLocked}");


                            return await SendAskForVoteConfirmationAsyncWithButtons(senderId, new List<string> { _defaultAskConfirmationImageUrl }, names);


                        }
                    }
                }



                var loggedInDB = await LogVoteRequestAsync(senderId, recipientId, msgId, userName, client.clientName, payload, decision);


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
                        //_logger.LogInformationWithCaller($"Vote from {senderId} for {client.clientName} recorded. Confirmation sent (if limit allows).");
                        return result;
                    }
                    else
                    {
                        // TODO Delete from DataBase
                        //result.SetError("Failed to send confirmation message.");
                        return result;

                    }

                }
                else
                {
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





        private async Task<OperationResult> LogClickerAccountAsync(string senderId, string recipientId, string MSGId, string userName, string voteName, string message, ClickerDecision decision = null)
		{
            var result = new OperationResult(false);
			var accountForBan = new BanAccount
			{
                Id = $"{senderId}.{DateTime.Now.ToString("yyyyMMddHHmmssfff")}",
                UserId = senderId,
				UserProvider = "FB",
				UserName = userName,
				IsSuspicious = decision.IsSuspicious,
				RiskScore = decision.RiskScore,
				Flags = decision.Flags,
				ShouldBlock = decision.ShouldBlock,
				Banned = true,
				BannedMsg = "BanMSG",
				BannedDate = DateTime.Now
			};

            try
            {
                await _dbContext.BannedAcount.AddAsync(accountForBan);
                await _dbContext.SaveChangesAsync();
                result.SetSuccess();
                result.Results = new { accountForBan.UserId, accountForBan.BannedDate};
            }
            catch (Exception ex)
            {
                result.SetError($"{ex}");
            }


            return result;
        }

        private async Task<OperationResult> LogVoteRequestAsync(string senderId, string recipientId, string MSGId, string userName, string voteName, string message, ClickerDecision? decision = null)
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
				CandidatePhone = string.IsNullOrEmpty(voteName) ? "" : _commonHelper.getCandidatePhone(voteName),
				Message = message == _voteStartFlag ? message : message.IndexOf(':') > -1 ? message : message + ":YES",
				

				IsSuspicious = decision == null ? null: decision.IsSuspicious,
				RiskScore = decision == null ? null : decision.RiskScore,
				Flags = decision == null ? null : decision.Flags,
				ShouldBlock = decision == null ? null : decision.ShouldBlock

			};

			try
			{
				await _dbContext.FaceBookVotes.AddAsync(newVote);
				await _dbContext.SaveChangesAsync();

                _metrics.IncRecsSavedInDB();
                _metrics.IncSavedInDBByCandidate(newVote.CandidateName);

                result.SetSuccess();
				result.Results = new { newVote.Id, newVote.Timestamp};
			}
			catch (Exception ex)
			{
				result.SetError($"{ex}");
                _metrics.IncErrorDBWhileSave();
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
			if (recipientId.StartsWith("TESTUSER"))
			{
				return res;
			}

			if (string.IsNullOrWhiteSpace(recipientId) || recipientId.Any(ch => !char.IsDigit(ch)))
			{
				res.SetError($"HTTP Request error while sending message to {recipientId}.");
				return res;
			}


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
				//_logger.LogInformationWithCaller($"Attempting to send message to {recipientId}.");

				// 4. Send the HTTP Request
				var response = await _httpClient.PostAsync(requestUrl, content);

				// 5. Handle the Response
				if (response.IsSuccessStatusCode)
				{
					//_logger.LogInformationWithCaller($"Message successfully sent to {recipientId}.");
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

			if (recipientId.StartsWith("TESTUSER"))
			{
				return res;
			}

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
        private static void ShuffleInPlace<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Shared.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private async Task<OperationResult> SendAskForVoteConfirmationAsyncWithButtons(string senderId, List<string> imageUrls, List<string> names)
		{
            var result = new OperationResult(true);


            // ✅ Safety: ensure same count & at least 3
            
            if (names.Count < 1)
            {
                result.SetError("Not enough candidates for confirmation payload.");
                return result;
            }

            // ✅ Pair (name, image) then shuffle order every time
            var candidates = Enumerable.Range(0, names.Count)
                .Select(i => new { Name = names[i]})
                .ToList();

            ShuffleInPlace(candidates); // <-- randomizes order



            var elements = new List<object>();
            var elements1 = new List<object>();
            //_logger.LogWarningWithCaller($"image from  {imageUrls[0]}, Sending Vote Confimation Request ");
            DateTime catchDateTime = DateTime.Now;
            //var stamp = catchUtc.ToString("yyyyMMddHHmmss");

            var buttonsList = new List<object>();

            foreach (var candidate in candidates)
            {
                buttonsList.Add(new
                {
                    type = "postback",
                    title = candidate.Name,
                    payload = $"{candidate.Name}_{catchDateTime}_YES"
                });
            }

            elements.Add(new
            {
                title = $"დაადასტურეთ არჩევანი მაქსიმუმ {_voteConfirmationDuration} წამში",
                image_url = imageUrls[0],
                // .ToArray() გადაიყვანს სიას იმ ფორმატში, რასაც API ელოდება
                buttons = buttonsList.ToArray()
            });

            elements1.Add(new
            {
                
                title = $"დაადასტურეთ არჩევანი მაქსიმუმ {_voteConfirmationDuration} წამში", //name,
                image_url = imageUrls[0],
                buttons = new[]
                    {
                    new
                    {
                        type = "postback",
                        title = $"{candidates[0].Name}",						
                        payload = $"{candidates[0].Name}_{catchDateTime}_YES" 
                    },
                    new
                    {
                        type = "postback",
                        title = $"{candidates[1].Name}",						
                        payload = $"{candidates[1].Name}_{catchDateTime}_YES"
                    },
                    new
                    {
                        type = "postback",
                        title = $"{candidates[2].Name}",
                        payload = $"{candidates[2].Name}_{catchDateTime}_YES"
                    }
                }
            });


            
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

            result = await SendMessagePayLoadSafeAsync(senderId, jsonPayload);

            return result;
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




			
			var lockKeyForEnableVote = RedisKeys.FB.Native.NeedForVoteLock(recipientId, senderId); 

			
			var success = await _cacheService.AcquireLockAsync(lockKeyForEnableVote, _ttl.VoteCooldown);

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



			// 2. Build the Carousel Elements
			var elements = new List<object>();

			// Ensure we don't exceed Facebook's 10 element limit or mismatch list counts
			var maxElements = Math.Min(imageUrls.Count, names.Count);

			for (int i = 0; i < maxElements; i++)
			{
				var name = names[i];
				var imageUrl = imageUrls[i];
                
				//if (!await IsImageUrlValidAsync(imageUrl))
                //{
                //    _logger.LogWarningWithCaller($"Invalid image URL detected, fallback applied: {imageUrl}");
                //    imageUrl = _defaultCandidateImageUrl;
                //}

                elements.Add(new
				{
					// Note: Title is optional in the Generic Template, but recommended for clarity.
					title = "ჩემი არჩევანია", //name,
					image_url = imageUrl,
					buttons = new[]
					{
					new
					{
						type = "postback",
						title = $"{name} 👍", // Georgian: Your vote for {name}
						//title = $"{name} 🟢 👍", // Georgian: Your vote for {name}
                        payload = $"{name}:YES" // Payload is the candidate name, used by the worker
                    }
					//,
                    //new
                    //{
                    //    type = "postback",
                    //    title = $"{name} 🔴 👎", // Georgian: Your vote for {name}
                    //    payload = $"{name}:NO" // Payload is the candidate name, used by the worker
                    //}
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
					//_logger.LogInformationWithCaller($"Request for Candidats from {recipientId} for {userName} recorded. Candidates Gallery sent (if limit allows).");
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






		/// <summary>
		/// Retrieves the user's name, prioritizing the Redis cache to minimize Facebook Graph API calls.
		/// </summary>
		private async Task<OperationResult> GetUserNameAsync(string? userId, string? recipientId)
		{
			var result = new OperationResult(false);
			//if (string.IsNullOrEmpty(userId)) return "უცნობი მომხმარებელი"; // Georgian for "Unknown User"

			if (string.IsNullOrWhiteSpace(userId))
			{
				result.SetError("UNKNOWN");
				return result;
				//return userId ?? "UNKNOWN";
			}



			//var cacheKey = $"userName:{userId}";
			var cacheKey = $"FB:Recipient:{recipientId}:User:{userId}:Name";


			if (userId.StartsWith("TESTUSER"))
			{
				var cacheTestKey = $"FB:Recipient:{recipientId}:User:{userId}:Name";
				var userTestName = await _cacheService.GetAsync<string>(cacheTestKey);
				await _cacheService.SetAsync(cacheTestKey, userTestName, TimeSpan.FromDays(7));
				if (!string.IsNullOrEmpty(userTestName))
				{
					// Cache Hit! Skip the slow external API call.
					result.SetSuccess(userTestName);
					return result;
					//return userTestName;
				}
				result.SetError("UNKNOWN");
				return result;
				//return userId ?? "UNKNOWN";
			}

			// 1. Check Cache
			var userName = await _cacheService.GetAsync<string>(cacheKey);



			if (!string.IsNullOrEmpty(userName))
			{
				// Cache Hit! Skip the slow external API call.
				result.SetSuccess(userName);
				return result;

				//return userName;
			}




			try
			{

				// [RATE LIMIT CHECK] Check Rate Limit BEFORE making the API call (GET request)
				if (await _rateLimitingService.IsRateLimitExceeded("GraphAPI:GetUserName"))
				{
					_logger.LogWarningWithCaller($"[RATE LIMIT BLOCKED] Cannot fetch user name for {userId}. Limit exceeded.");
					result.SetError($"უცნობი მომხმარებელი, {userId}. Limit exceeded.");
					return result;
					//return $"უცნობი მომხმარებელი, {userId}. Limit exceeded."; // Fail safe: return unknown name
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

					result.SetSuccess(userName);
					return result;
					//return userName;
				}
				else
				{
					_logger.LogErrorWithCaller($"Failed to fetch user name for {userId}. Status: {response.StatusCode}, ResquestUri {response.RequestMessage?.RequestUri?.AbsoluteUri}");
					result.SetError("უცნობი მომხმარებელი");
					return result;
					//return "უცნობი მომხმარებელი";
					
				}
			}
			catch (Exception ex)
			{
				_logger.LogErrorWithCaller($"Error calling Graph API for user {userId}. {ex}");
				result.SetError("უცნობი მომხმარებელი");
				return result;
				//return "უცნობი მომხმარებელი"; // Fallback name
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





        #region ProcessPostbackAsync_TryCatchClicker_Alter

        // =========================================
        // ProcessPostbackAsync_TryCatchClicker_Alter
        // Refactor: readable + helper methods
        // =========================================
        //
        // იდეა:
        // 1) ProcessingLock (მოკლე) — concurrency/double-processing დაცვა (არ ანაცვლებს messageId idempotency-ს)
        // 2) CooldownLock (გრძელი) — ირთვება მხოლოდ მაშინ, როცა vote რეალურად ჩაიწერა (Normal accept ან Confirm YES)
        // 3) Confirmed payload-ზე clicker არ მოწმდება, მაგრამ PendingConfirm უნდა არსებობდეს (ანტი-forgery + expiry)
        // 4) Suspicious შემთხვევაში — DB-ში არ ვწერთ, ვქმნით PendingConfirm-ს და ვუგზავნით confirmation UI-ს
        //
        // NOTE: აქ PendingConfirm key-ში მინიმუმად string ვინახავთ.
        // უკეთესი: JSON (candidateId + createdUnix + riskScore + flags). სურვილისამებრ შემდეგ ეტაპზე დავამატოთ.

        private async Task<OperationResult> ProcessPostbackAsync_TryCatchClicker_Alter(
            string senderId,
            string recipientId,
            string msgId,
            string userName,
            string payload)
        {
            var result = new OperationResult(true);

            // --- 0) Parse & Validate vote target ---
            var voteName = _payloadHelper.ExtractVoteName(payload);
            if (!TryResolveClient(voteName, out var client))
            {
                _logger.LogWarningWithCaller($"Postback received for unknown client: {voteName}");
                result.SetError($"Unknown client: {voteName}");
                return result;
            }

            // --- 1) Acquire SHORT processing lock (anti-parallel) ---
            var processLockKey = RedisKeys.FB.Native.BuildProcessLockKey(recipientId, senderId);
            if (!await TryAcquireProcessingLock(processLockKey))
            {
                _logger.LogWarningWithCaller($"ProcessLock busy. Ignored. key={processLockKey}");
                result.SetError("Duplicate processing ignored.");
                return result;
            }

            try
            {
                // Common keys used in both paths
                var pendingKey = RedisKeys.FB.Native.BuildPendingConfirmKey(recipientId, senderId);
                var cooldownLockKey = RedisKeys.FB.Native.VoteLock(recipientId, senderId); // Cooldown lock ONLY

                // --- 2) Confirm path (payload already contains "Confirmed") ---
                if (IsConfirmedPayload(payload))
                {
                    return await HandleConfirmedFlow(
                        senderId, recipientId, msgId, userName,
                        payload, voteName, client.clientName,
                        pendingKey, cooldownLockKey);
                }

                // --- 3) Normal path: clicker evaluation / maybe ask confirmation ---
                var decision = await EvaluateClickerCheckIfEnabled(recipientId, senderId, userName, client.clientName);

                // Suspicious => ask confirmation (NO DB write, NO cooldown lock)
                if (ShouldAskConfirmation(decision))
                {
                    return await HandleSuspiciousAskConfirmation(
                        senderId, recipientId, msgId, userName,
                        payload, voteName, client.clientName,
                        decision, pendingKey);
                }

                // --- 4) Accept normal vote (cooldown -> DB -> message) ---
                return await AcceptVoteWithCooldown(
                    senderId, recipientId, msgId, userName,
                    payload, voteName, client.clientName,
                    decision, cooldownLockKey);
            }
            finally
            {
                await ReleaseProcessingLock(processLockKey);
            }
        }

        // =====================================================
        // Helpers
        // =====================================================



        /// <summary>
        /// Finds registered client by voteName.
        /// </summary>
        private bool TryResolveClient(string voteName, out dynamic client)
        {
            client = _registeredClients.FirstOrDefault(c =>
                c.clientName.Equals(voteName, StringComparison.OrdinalIgnoreCase));
            return client != null;
        }



        /// <summary>
        /// Acquire processing lock. TTL small because it only protects concurrent processing.
        /// </summary>
        private async Task<bool> TryAcquireProcessingLock(string processLockKey)
        {
            // 3 seconds is usually enough for one pipeline pass.
            // If your processing may take longer, bump to 5 sec.
            return await _cacheService.AcquireLockAsync(processLockKey, TimeSpan.FromSeconds(3));
        }

        private async Task ReleaseProcessingLock(string processLockKey)
        {
            await _cacheService.ReleaseLockAsync(processLockKey);
        }

        /// <summary>
        /// Confirmed payload detection. You said you pass payload with "Confirmed" suffix.
        /// </summary>
        private bool IsConfirmedPayload(string payload)
            => payload.Contains("Confirmed", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Evaluates clicker decision only when enabled. Also updates live metrics.
        /// </summary>
        private async Task<ClickerDecision> EvaluateClickerCheckIfEnabled(
            string recipientId,
            string senderId,
            string userName,
            string clientName)
        {
            var decision = new ClickerDecision();

            if (!_checkForClickers)
                return decision;

            decision = await _clickerDetection.EvaluateAsync(recipientId, senderId, userName, DateTime.UtcNow);

            // UI metrics update (already in your system)
            await _clickerDetection.ApplyClikerMetrics(clientName, decision);


			//decision.ShouldAskConfirmation = true;
            return decision;
        }

        /// <summary>
        /// In your current logic "ShouldBlock == true" is used to trigger confirmation flow.
        /// Keep the same behavior here.
        /// </summary>
        private bool ShouldAskConfirmation(ClickerDecision decision)
        {
            return (decision?.ShouldAskConfirmation == true && _sendVoteConfirmationToBadUsers) || _sendVoteConfirmationToAllUsers;
        }

        /// <summary>
        /// Handles suspicious user confirmation request:
        /// - logs clicker
        /// - stores pending confirmation for 120 sec
        /// - sends confirm UI
        /// IMPORTANT: no cooldown lock here, no DB write here.
        /// </summary>
        private async Task<OperationResult> HandleSuspiciousAskConfirmation(
            string senderId,
            string recipientId,
            string msgId,
            string userName,
            string payload,
            string voteName,
            string clientName,
            ClickerDecision decision,
            string pendingKey)
        {
            // Log suspicious account (audit)
			if (_checkForClickers)
				await LogClickerAccountAsync(senderId, recipientId, msgId, userName, clientName, payload, decision);

            // Store pending confirm (TTL = vote confirmation duration)
            // NOTE: minimally store voteName. Better: store JSON with candidateId+flags+score.
            await _cacheService.SetAsync(pendingKey, voteName, TimeSpan.FromSeconds(_voteConfirmationDuration));

            // Send confirmation UI
            var names = new List<string> { voteName, "-", "-" };
            return await SendAskForVoteConfirmationAsyncWithButtons(
                senderId,
                new List<string> { _defaultAskConfirmationImageUrl },
                names);
        }

        /// <summary>
        /// Confirmed flow:
        /// - must have pending confirmation
        /// - YESConfirmed => acquire cooldown lock, write DB, delete pending, send message
        /// - NOConfirmed  => delete pending, send cancelled message (no DB, no cooldown)
        /// </summary>
        private async Task<OperationResult> HandleConfirmedFlow(
            string senderId,
            string recipientId,
            string msgId,
            string userName,
            string payload,
            string voteName,
            string clientName,
            string pendingKey,
            string cooldownLockKey)
        {
            var result = new OperationResult(true);

            // 1) Pending must exist (anti-forgery + expiry check)
            var pending = await _cacheService.GetAsync<string>(pendingKey);
            if (string.IsNullOrEmpty(pending))
            {
                _logger.LogWarningWithCaller($"Confirmed vote but pending not found/expired. sender={senderId}, vote={voteName}");
                result.SetError("Vote Confirmation expired (no pending).");
                return result;
            }

            // 2) Parse YES/NO from payload: "ClientName:YESConfirmed" / "ClientName:NOConfirmed"
            var action = ExtractConfirmAction(payload);

            if (!action.IsYes)
            {
                // NO => delete pending, do not start cooldown, do not write DB
                await SoftDeleteKey(pendingKey);
                var cancelMsg = "გაუქმდა. თქვენი ხმა არ ჩაითვალა.";
                result = await SendMessageAsync(senderId, userName, cancelMsg);
                if (!result.Result) result.SetError("Failed to send cancellation message.");
                return result;
            }

            // YES => now it becomes a real accepted vote => start cooldown
            var gotCooldown = await _cacheService.AcquireLockAsync(cooldownLockKey, _ttl.VoteCooldown);
            _logger.LogWarningWithCaller($"CooldownLock {cooldownLockKey} after AcquireLockAsync (CONFIRMED)");

            if (!gotCooldown)
            {
                bool sendNotAcceptedVoteBackInfo = await _varsKeeper.GetValueAsync<bool>("fb_NotAcceptedVoteBackInfo");
                if (sendNotAcceptedVoteBackInfo)
                    await SendMessageAsync(senderId, userName, $"თქვენი ბოლო ხმის მიცემიდან არ გასულა {_voteMinuteRange} წუთი.");

                result.SetError("Vote denied due to rate limit (cooldown).");
                return result;
            }

            // Write to DB (flags already stored inside decision if your LogVoteRequestAsync persists them)
            // If you want the *original* suspicious flags, store them in pending JSON and load here.
            var decisionConfirmed = new ClickerDecision(); // optional marker/flag can be added
            var loggedInDB = await LogVoteRequestAsync(senderId, recipientId, msgId, userName, clientName, payload, decisionConfirmed);

            if (!loggedInDB.Result)
            {
                result.SetError("Failed to log confirmed vote in database.");
                return result;
            }

            // Pending must be removed to prevent double-accept
            await SoftDeleteKey(pendingKey);

            // Send success msg
            return await SendAcceptedVoteMessage(senderId, userName, clientName, loggedInDB);
        }

        /// <summary>
        /// Normal accept flow:
        /// - cooldown lock first
        /// - then DB write
        /// - then success message
        /// </summary>
        private async Task<OperationResult> AcceptVoteWithCooldown(
            string senderId,
            string recipientId,
            string msgId,
            string userName,
            string payload,
            string voteName,
            string clientName,
            ClickerDecision decision,
            string cooldownLockKey)
        {
            var result = new OperationResult(true);

            var gotCooldown = await _cacheService.AcquireLockAsync(cooldownLockKey, _ttl.VoteCooldown);
            //_logger.LogWarningWithCaller($"CooldownLock {cooldownLockKey} after AcquireLockAsync (NORMAL)");

            if (!gotCooldown)
            {
                _logger.LogWarningWithCaller($"Vote from {senderId} for {voteName} NOT registered due to rate limit ({_voteMinuteRange} min).");

                bool sendNotAcceptedVoteBackInfo = await _varsKeeper.GetValueAsync<bool>("fb_NotAcceptedVoteBackInfo");
                if (sendNotAcceptedVoteBackInfo)
                    await SendMessageAsync(senderId, userName, $"თქვენი ბოლო ხმის მიცემიდან არ გასულა {_voteMinuteRange} წუთი.");

                result.SetError("Vote denied due to rate limit.");
                return result;
            }

            var loggedInDB = await LogVoteRequestAsync(senderId, recipientId, msgId, userName, clientName, payload, decision);
            if (!loggedInDB.Result)
            {
                result.SetError("Failed to log vote in database.");
                return result;
            }

            return await SendAcceptedVoteMessage(senderId, userName, clientName, loggedInDB);
        }

        /// <summary>
        /// Extract YES/NO from "ClientName:YESConfirmed" payload.
        /// </summary>
        private (bool IsYes, string Raw) ExtractConfirmAction(string payload)
        {
            // payload: "ClientName:YESConfirmed"
            var parts = payload.Split(':');
            if (parts.Length < 2) return (false, "");

            var actionPart = parts[1];
            var isYes = actionPart.StartsWith("YES", StringComparison.OrdinalIgnoreCase);
            return (isYes, actionPart);
        }

        /// <summary>
        /// Because your cache service doesn't show DeleteAsync, we do a "soft delete":
        /// write null with tiny TTL. If you can add Delete/Remove method, replace this.
        /// </summary>
        private async Task SoftDeleteKey(string key)
        {
            await _cacheService.SetAsync<string>(key, null, TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Creates and sends "vote accepted" message, based on DB timestamp returned by LogVoteRequestAsync.
        /// </summary>
        private async Task<OperationResult> SendAcceptedVoteMessage(
            string senderId,
            string userName,
            string clientName,
            OperationResult loggedInDB)
        {
            var result = new OperationResult(true);

            // Expecting loggedInDB.Results.Timestamp
            var tsProp = loggedInDB.Results?.GetType().GetProperty("Timestamp");
            if (tsProp == null)
            {
                result.SetError("Logged vote result missing Timestamp.");
                return result;
            }

            var timestamp = (DateTime)tsProp.GetValue(loggedInDB.Results);
            var nextVoteTime = timestamp.AddMinutes(_voteMinuteRange);

            var backMsg =
                $"თქვენი ხმა {clientName}-სთვის მიღებულია! მადლობა. ჩვენ კვლავ მივიღებთ თქვენს ხმას {_voteMinuteRange} წუთის მერე, {nextVoteTime} -დან";

            result = await SendMessageAsync(senderId, userName, backMsg);
            return result;
        }


        #endregion

    }

}
