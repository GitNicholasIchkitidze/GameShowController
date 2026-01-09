
using GameController.FBService.Extensions;
using GameController.FBService.Heplers;
using GameController.FBService.Models;
using GameController.FBService.Services;
using Humanizer;
using Humanizer.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using System.Net.Http;
using System.Text;
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
	private readonly IAppMetrics _metrics;


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
		IAppMetrics metrics,                 // ADDED
		IWebhookProcessorService webhookProcessorService)
	{
		_logger = logger;
		_messageQueueService = messageQueueService;
		_dbContext = dbContext;
		_varsKeeper = varsKeeper;
		_metrics = metrics; // ADDED

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
	public async Task<IActionResult> HandleWebhook()
	{
		// Goal: return 200 OK as fast as possible under heavy load (300-1000 msg/sec),
		// and offload ALL parsing/idempotency/listening checks to the worker pipeline.
		try
		{

			_metrics.IncIngress();
			bool isListening = await _varsKeeper.GetValueAsync<bool>("fb_listening_active");
			if (!isListening)
			{
				//await Request.Body.CopyToAsync(Stream.Null);
				return Ok();
			}

			if (_messageQueueService.IsNearFull)
				return Ok(); // ✅ drop early, no body read

			// Read raw request body (no model-binding / JsonObject allocation).
			using var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 32 * 1024, leaveOpen: true);
			var rawPayload = await reader.ReadToEndAsync();

			if (string.IsNullOrWhiteSpace(rawPayload))
			{
				// Nothing to process; still ACK to Facebook.
				return Ok();
			}
			_messageQueueService.TryEnqueueMessage(rawPayload);
		}
		catch (Exception ex)
		{
			// Never fail the webhook endpoint. Always ACK to avoid FB retries/storms.
			//_logger.LogErrorWithCaller($"Webhook ingress failed (still ACK). {ex.Message}");
		}

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
		//var analytics = AnalyzeVotesWithYesAndNo(votes);

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


	private object AnalyzeVotesWithYesAndNo(List<Vote> votes)
	{
		var totalVotes = votes.Count;
		var totalUniqueUsers = votes.Select(v => v.UserName).Distinct().Count();

		var groupedVotes = votes
			.GroupBy(v => v.Message?.Trim().ToUpperInvariant() + "\t" + v.CandidatePhone?.Trim().ToUpperInvariant())
			.Select(g => new
			{
				Option = g.Key,
				VoteCount = g.Count(),
				VoteCountYes = g.Count(v => v.Message != null && v.Message.Trim().EndsWith(":YES", StringComparison.OrdinalIgnoreCase)),
				VoteCountNo = g.Count(v => v.Message != null && v.Message.Trim().EndsWith(":NO", StringComparison.OrdinalIgnoreCase)),
				Percentage = totalVotes > 0 ? (double)g.Count() / totalVotes * 100 : 0,
				UniqueUsers = g.Select(v => v.UserName).Distinct().Count(),
				TopUsers = g.GroupBy(v => v.UserName)
							.Select(ug => new { User = ug.Key, Count = ug.Count() })
							.OrderByDescending(x => x.Count)
							.Take(5)
							.ToList()
			})
			.OrderByDescending(x => x.VoteCount)
			.ToList();

		return new
		{
			TotalVotes = totalVotes,
			TotalUniqueUsers = totalUniqueUsers,
			Options = groupedVotes
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













