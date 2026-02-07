
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
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
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



    // =========================
    // ✅ Meta Data Deletion Config
    // =========================
    private readonly string _metaAppSecret; // Meta App Secret (for signed_request validation)
    private readonly string _baseUrl;       // https://your-domain.com (for status url)
    private const string DeletionStatusPrefix = "fb_delete_status:";


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


        _metaAppSecret =
            configuration.GetValue<string>("Meta:AppSecret")
            ?? configuration.GetValue<string>("Facebook:AppSecret")
            ?? configuration.GetValue<string>("appSecret")
            ?? "";

        _baseUrl =
            configuration.GetValue<string>("Meta:BaseUrl")
            ?? configuration.GetValue<string>("Facebook:BaseUrl")
            ?? configuration.GetValue<string>("baseUrl")
            ?? "";

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




    public class FbDataDeletionDto
    {
        public string signed_request { get; set; } = "";
    }

    // ==========================================================
    // ✅ Meta User Data Deletion Callback (IMPORTANT)
    // ==========================================================
    // Configure this URL in Meta App Dashboard:
    // Data Deletion Requests -> Data Deletion Callback URL:
    // https://your-domain.com/api/facebookwebhooks/data-deletion
    [HttpPost("data-deletion")]
    //[Consumes("application/x-www-form-urlencoded", "application/json")]
    //public async Task<IActionResult> DataDeletionCallback()

    [Consumes("application/json")]

    public async Task<IActionResult> DataDeletionCallback([FromBody] FbDataDeletionDto dto)
    {
        var signedRequest = dto?.signed_request;

        if (string.IsNullOrWhiteSpace(signedRequest))
        {
            _logger.LogWarningWithCaller("DataDeletionCallback: signed_request missing.");
            return BadRequest("signed_request is required.");
        }

        if (string.IsNullOrWhiteSpace(_metaAppSecret))
        {
            _logger.LogErrorWithCaller("DataDeletionCallback: Meta AppSecret is missing in configuration.");
            return StatusCode(500, "AppSecret not configured.");
        }

        try
        {
            var payload = TryParseSignedRequest(signedRequest, _metaAppSecret, out var error);
            if (payload == null)
            {
                _logger.LogWarningWithCaller($"DataDeletionCallback: invalid signed_request. Error={error}");
                return BadRequest("invalid signed_request");
            }

            var userId = payload.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarningWithCaller("DataDeletionCallback: user_id missing in payload.");
                return BadRequest("user_id missing");
            }

            var confirmationCode = $"del_{userId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

            await _varsKeeper.SetValueAsync(DeletionStatusPrefix + confirmationCode, "pending");

            try
            {
                await DeleteUserDataByUserIdAsync(userId);
                await _varsKeeper.SetValueAsync(DeletionStatusPrefix + confirmationCode, "completed");

                _logger.LogInformationWithCaller(
                    $"DataDeletionCallback: deletion completed. userId={userId}, code={confirmationCode}");
            }
            catch (Exception exDel)
            {
                await _varsKeeper.SetValueAsync(DeletionStatusPrefix + confirmationCode, "failed");
                _logger.LogErrorWithCaller(
                    $"DataDeletionCallback: deletion FAILED. userId={userId}, code={confirmationCode}, {exDel.Message}");
            }

            var baseUrl = _baseUrl?.TrimEnd('/') ?? "";
            var statusUrl =
                $"{baseUrl}/api/facebookwebhooks/data-deletion/status/{confirmationCode}";

            return new JsonResult(new
            {
                url = statusUrl,
                confirmation_code = confirmationCode
            });
        }
        catch (Exception ex)
        {
            _logger.LogErrorWithCaller($"DataDeletionCallback: unexpected error, {ex.Message}");
            return StatusCode(500, "Server error");
        }
    }


    [HttpGet("data-deletion/status/{code}")]
    public async Task<IActionResult> DataDeletionStatus([FromRoute] string code)
    {
        try
        {
            var status = await _varsKeeper.GetValueAsync<string>(DeletionStatusPrefix + code);

            if (string.IsNullOrWhiteSpace(status))
                status = "not_found";

            return Ok(new
            {
                confirmation_code = code,
                status
            });
        }
        catch (Exception ex)
        {
            _logger.LogErrorWithCaller($"DataDeletionStatus failed for code={code}, {ex.Message}");
            return StatusCode(500, "Server error");
        }
    }

    // ✅ რეალური წაშლა DB-დან (UserId = AppScopedUserId)
    private async Task DeleteUserDataByUserIdAsync(string userId)
    {
        // 1) FaceBookVotes table (your shown usage: _dbContext.FaceBookVotes)
        // NOTE: assuming Vote entity has UserId column/property. If it differs, change here.
        var votes = await _dbContext.FaceBookVotes
            .Where(v => v.UserId == userId)
            .ToListAsync();

        if (votes.Count > 0)
        {
            _dbContext.FaceBookVotes.RemoveRange(votes);
            await _dbContext.SaveChangesAsync();
        }

        // 2) TODO: delete from other tables that store user data (if you have them)
        // Example:
        // var msgs = await _dbContext.FaceBookMessages.Where(m => m.UserId == userId).ToListAsync();
        // _dbContext.FaceBookMessages.RemoveRange(msgs);
        // await _dbContext.SaveChangesAsync();

        // 3) TODO: Redis/user-specific cache cleanup
        // If you have user-bound Redis keys, delete them here via your cache service.
        // (I didn't add new DI dependency to keep your controller compiling without extra changes.)
    }

    // Signed Request parsing (HMAC-SHA256)
    private static SignedRequestPayload? TryParseSignedRequest(string signedRequest, string appSecret, out string error)
    {
        error = "";

        var parts = signedRequest.Split('.', 2);
        if (parts.Length != 2)
        {
            error = "signed_request format invalid";
            return null;
        }

        var encodedSig = parts[0];
        var encodedPayload = parts[1];

        byte[] sig;
        try
        {
            sig = Base64UrlDecode(encodedSig);
        }
        catch
        {
            error = "signature base64 invalid";
            return null;
        }

        // IMPORTANT: signature is computed over the BASE64URL-encoded payload (the 2nd part as-is)
        var payloadBytesForHmac = Encoding.UTF8.GetBytes(encodedPayload);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var expected = hmac.ComputeHash(payloadBytesForHmac);

        if (!CryptographicOperations.FixedTimeEquals(sig, expected))
        {
            error = "signature mismatch";
            return null;
        }

        string json;
        try
        {
            json = Encoding.UTF8.GetString(Base64UrlDecode(encodedPayload));
        }
        catch
        {
            error = "payload base64 invalid";
            return null;
        }

        SignedRequestPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<SignedRequestPayload>(json);
        }
        catch
        {
            error = "payload json invalid";
            return null;
        }

        if (!string.Equals(payload?.Algorithm, "HMAC-SHA256", StringComparison.OrdinalIgnoreCase))
        {
            error = "unexpected algorithm";
            return null;
        }

        return payload;
    }

    private static byte[] Base64UrlDecode(string input)
    {
        input = input.Replace('-', '+').Replace('_', '/');
        switch (input.Length % 4)
        {
            case 2: input += "=="; break;
            case 3: input += "="; break;
        }
        return Convert.FromBase64String(input);
    }

    private class SignedRequestPayload
    {
        [JsonPropertyName("algorithm")]
        public string? Algorithm { get; set; }

        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }
    }

    // ==========================================================
    // 🔧 DEV ONLY: Generate signed_request for Swagger testing
    // ==========================================================
    [HttpPost("data-deletion/dev-generate-signed-request")]
    public IActionResult DevGenerateSignedRequest([FromQuery] string userId)
    {
        // ❌ უსაფრთხოება: PROD-ზე არ უნდა მუშაობდეს
        if (!HttpContext.RequestServices
            .GetService<IWebHostEnvironment>()!
            .IsDevelopment())
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest("userId is required.");

        if (string.IsNullOrWhiteSpace(_metaAppSecret))
            return StatusCode(500, "Meta AppSecret is not configured.");

        var signedRequest = CreateSignedRequest(_metaAppSecret, userId);

        return Ok(new
        {
            userId,
            signed_request = signedRequest
        });
    }



    private static string CreateSignedRequest(string appSecret, string userId)
    {
        var payload = new
        {
            algorithm = "HMAC-SHA256",
            user_id = userId,
            issued_at = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadBase64 = Base64UrlEncode1(Encoding.UTF8.GetBytes(payloadJson));

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadBase64));
        var signatureBase64 = Base64UrlEncode1(signature);

        return $"{signatureBase64}.{payloadBase64}";
    }

    private static string Base64UrlEncode1(byte[] input)
    {
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
    // ==========================================================
    // Existing endpoints (unchanged)
    // ==========================================================




}













