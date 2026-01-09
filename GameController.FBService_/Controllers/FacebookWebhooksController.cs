
using GameController.FBService.Extensions;
using GameController.FBService.Heplers;
using GameController.FBService.Models;
using GameController.FBService.Services;
using Humanizer;
using Humanizer.Configuration;
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
	private readonly ILogger<FacebookWebhooksController> _logger;//


	private List<FBClientConfiguration> _registeredClients;
	private readonly string _verifyToken;
	private readonly IMessageQueueService _messageQueueService;
	private readonly IDempotencyService _dempotencyService;
	private readonly ApplicationDbContext _dbContext;
	//private readonly string _voteStartFlag;
	

	private readonly IWebhookProcessorService _webhookProcessorService;

	private readonly IGlobalVarsKeeper _varsKeeper;
	private const string ListeningKey = "fb_listening_active";
	private const string NotAcceptedVoteBackInfo = "fb_NotAcceptedVoteBackInfo";
    private readonly string _voteStartFlag;



    public FacebookWebhooksController(
		ILogger<FacebookWebhooksController> logger,
		ApplicationDbContext dbContext,
		IConfiguration configuration,
		IMessageQueueService messageQueueService,
		IDempotencyService dempotencyService,
		IGlobalVarsKeeper varsKeeper,
		IWebhookProcessorService webhookProcessorService)
	{
		_logger = logger;
		_messageQueueService = messageQueueService;
		_dbContext = dbContext;
		_varsKeeper = varsKeeper;

		_dempotencyService = dempotencyService;
		_webhookProcessorService = webhookProcessorService;
		_verifyToken = configuration.GetValue<string>("verifyToken") ?? "myFbToken";
		_voteStartFlag = configuration.GetValue<string>("voteStartFlag") ?? "";
	}

	[HttpGet]
	public IActionResult VerifyWebhook([FromQuery(Name = "hub.mode")] string mode,
									   [FromQuery(Name = "hub.verify_token")] string token,
									   [FromQuery(Name = "hub.challenge")] string challenge)
	{
		_logger.LogInformationWithCaller($"Mode: {mode}, Received Token: {token}, Challenge: {challenge}");
		_logger.LogInformationWithCaller($"Expected Token: {_verifyToken}");

		if (mode == "subscribe" && token == _verifyToken)
		{
			_logger.LogInformationWithCaller($"Verified WebHook");
			return Content(challenge, "text/plain"); // 
			//return Ok(challenge);
		}
		_logger.LogInformationWithCaller("Verification failed - Mode or Token Mismatch.");

		return BadRequest("Verification failed.");
	}

	[HttpPost]
	public async Task<IActionResult> HandleWebhook([FromBody] JsonObject payload)
	{

        if (payload is null)
        {
            _logger.LogWarningWithCaller("Received null payload in webhook.");
            return BadRequest("Payload is required.");
        }

        bool isListening = await _varsKeeper.GetValueAsync<bool>("fb_listening_active");

		if (!isListening)
		{
			_logger.LogWarningWithCaller("WebHook Handled,  but not Listening To FaceBook now");
			return Ok();

		}

		

        var messageType = _webhookProcessorService.ExtractMessageType(payload);
        if (string.IsNullOrEmpty(messageType))
        {
            _logger.LogWarningWithCaller("Message type not found in payload, skipping idempotency check. Exit to FB");
            return Ok();
        }
        if (messageType.Equals("message", StringComparison.OrdinalIgnoreCase))
        {
            var text = _webhookProcessorService.ExtractMessageText(payload);
            if (!string.Equals(text, _voteStartFlag, StringComparison.OrdinalIgnoreCase))
            {
                // IMPORTANT: do NOT log warning here; too frequent under load
                //_logger.LogWarningWithCaller($"Ignoring non-voteStart message. Text= {text}");
                return Ok();
            }
        }
        else if (!messageType.Equals("postback", StringComparison.OrdinalIgnoreCase))
        {
            // Ignore all other event types
            return Ok();
        }


        //_logger.LogInformationWithCaller($"PayLoad Received: {payload}");
        var messageId = _webhookProcessorService.ExtractMessageId(payload, messageType);
		

		

		if (string.IsNullOrEmpty(messageId))
		{
			_logger.LogWarningWithCaller("Message ID not found in payload, skipping idempotency check. Exit to FB");
			return Ok();
		}


		if (await _dempotencyService.IsDuplicateAsync(messageId))
		{
			_logger.LogInformationWithCaller($"Duplicate Facebook message ignored: {messageId}");
			return Ok(); 
		}
	
	
		try
		{
			// CRITICAL STEP: Offload the raw payload string to the worker queue
			await _messageQueueService.EnqueueMessageAsync(payload.ToString());
		}
		catch (Exception ex)
		{
			// Log the queuing failure but still return 200 OK to Facebook
			_logger.LogErrorWithCaller($"Failed to enqueue message: {ex.Message}");
		}


		// MUST return 200 OK immediately to satisfy Facebook's 20-second timeout.
		return Ok();
	}


	
	[HttpGet("GetFBVotes")]

	public async Task<IActionResult> GetVotesAsync(DateTime? fromDate, DateTime? toDate)
	{
		fromDate ??= DateTime.UtcNow.Date;
		toDate ??= DateTime.UtcNow.AddDays(1); 

		 

		var allVotes = await _dbContext.FaceBookVotes
			.Where(v => v.Timestamp >= fromDate && v.Timestamp <= toDate && !string.IsNullOrEmpty(v.CandidateName))
			.OrderByDescending(v => v.Timestamp)
			//.Take(200)
			.ToListAsync();

		var analytics = AnalyzeVotes(allVotes);

		return new JsonResult(new
		{
			votes = analytics

		});
	}


	private object AnalyzeVotes(List<Vote> votes)
	{
		var totalVotes = votes.Count;
		var totalUniqueUsers = votes.Select(v => v.UserName).Distinct().Count();

		var groupedVotes = votes
			.GroupBy(v => v.Message?.Trim().ToUpperInvariant() + "\t" + v.CandidatePhone?.Trim().ToUpperInvariant())
			.Select(g => new
			{
				Option = g.Key,
				VoteCount = g.Count(), // 3) ხმის რაოდენობა
				UniqueUsers = g.Select(v => v.UserName).Distinct().Count(), // 4) უნიკალური მომხმარებელი თითოეულ ვარიანტზე

				// 5) ტოპ 3 მომხმარებელი თითოეულ ვარიანტზე
				TopUsers = g
					.GroupBy(v => v.UserName)
					.Select(u => new
					{
						UserName = u.Key,
						UserVoteCount = u.Count()
					})
					.OrderByDescending(u => u.UserVoteCount)
					.Take(3)
			})
			.OrderByDescending(a => a.VoteCount)
			.ToList();

		return new
		{
			TotalVotes = totalVotes, // 1) საერთო ხმების რაოდენობა
			TotalUniqueUsers = totalUniqueUsers, // 2) საერთო უნიკალური მომხმარებლის რაოდენობა
			Options = groupedVotes.Select(g => new
			{
				g.Option,
				g.VoteCount,
				Percentage = totalVotes > 0 ? (double)g.VoteCount / totalVotes * 100 : 0, // 3) პროცენტულობა
				g.UniqueUsers,
				g.TopUsers
			}).ToList()
		};
	}

	[HttpGet("CheckVotingModeStatus")]
	public async Task<bool> IsListeningAsync()
	{
		return await _varsKeeper.GetValueAsync<bool>(ListeningKey);
	}

	[HttpPost("SetVotingModeStatus")]
	public async Task SetListeningAsync(bool active)
	{
		await _varsKeeper.SetValueAsync(ListeningKey, active);
	}

	[HttpPost("SetBooleanKeyValue")]
	public async Task SetKeyBooleanValue(string key, bool active)
	{
		await _varsKeeper.SetValueAsync(key, active);
	}

	[HttpGet("AllSetVotingModeStatus")]
	public async Task<IActionResult> GetAllKeysAsync(string? pattern = null)
	{
		return new JsonResult(new
		{
			//Keys = await _varsKeeper.GetAllKeysAsync(pattern)
				Keys_Vals = await _varsKeeper.GetAllKeysAndValuesAsync(pattern)

		});

		
	}

}













