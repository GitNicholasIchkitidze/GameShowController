using GameController.FBService.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameController.FBService.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class HealthController : ControllerBase
	{
		private readonly IMessageQueueService _queue;
		private readonly IAppMetrics _metrics; // ADDED


		public HealthController(IMessageQueueService queue, IAppMetrics metrics)
		{
			_queue = queue;
			_metrics = metrics;
		}

		[HttpGet("health")]
		public IActionResult Health()
		{
			return Ok(new
			{
				status = "ok",
				serverTime = DateTime.UtcNow,
				machine = Environment.MachineName
			});
		}

		[HttpPost("loadtest")]
		public IActionResult LoadTest()
		{
			_queue.TryEnqueueMessage("1");
			return Ok();
		}

		[HttpGet("queue")]
		public IActionResult Queue()
		{
			return Ok(new
			{
				_queue.Capacity,
				_queue.CurrentDepth,
				_queue.DroppedCount
			});
		}

		[HttpGet("metrics")]
		public IActionResult Metrics()
		{
			return Ok(new
			{
				serverTime = DateTime.UtcNow,
				queue = new
				{
					_queue.Capacity,
					_queue.CurrentDepth,
					_queue.DroppedCount
				},
				counters = _metrics.Snapshot()
			});
		}

		[HttpGet("ishealth")]
		public IActionResult IsHealth() => Ok("ok");
	}
}
