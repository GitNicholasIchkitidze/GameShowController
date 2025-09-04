using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameController.Shared.Models.YouTube
{
	public class VoteSubmissionMessage
	{
		public string? UserId { get; set; }
		public string? UserName { get; set; }
		public string? UserChatHandle { get; set; }
		public string? Answer { get; set; }
		public DateTime Timestamp { get; set; }
	}
}
