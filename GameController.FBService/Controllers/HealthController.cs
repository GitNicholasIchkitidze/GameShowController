using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Channels;

namespace GameController.FBService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {

        private static readonly Channel<string> _ch = Channel.CreateUnbounded<string>();

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

        [HttpPost("_loadtest")]
        public IActionResult Post([FromBody] object payload)
        {
            // Fast ACK – აქ არანაირი DB/ლოგიკა
            _ = _ch.Writer.TryWrite("1");
            return Ok();
        }

        [HttpGet("ishealth")]
        public IActionResult IsHealth() => Ok("ok");

    }
}
