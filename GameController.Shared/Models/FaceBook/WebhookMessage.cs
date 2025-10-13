using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameController.Shared.Models.FaceBook
{
	public class WebhookMessage
	{
		public string SenderId { get; set; }
		public string ReceivedMessage { get; set; }
		public long TimeStamp { get; set; }
		public string PostBackPayload { get; set; }

		// Add other necessary data points from the original payload if needed
	}
}
