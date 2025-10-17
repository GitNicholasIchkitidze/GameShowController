using GameController.FBService.Extensions;
using GameController.FBService.Models;
using GameController.FBService.Services;
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




	public FacebookWebhooksController(
		ILogger<FacebookWebhooksController> logger,
		IConfiguration configuration,
		IMessageQueueService messageQueueService)
	{
		_logger = logger;
		_messageQueueService = messageQueueService;
		_verifyToken = configuration.GetValue<string>("verifyToken") ?? "myFbToken";
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
		_logger.LogInformationWithCaller($"PayLoad Received: {payload}");

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
}









