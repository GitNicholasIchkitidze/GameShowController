using GameController.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameController.Shared.Models.YouTube
{
	public class ChatMessage
	{
		public ChatMessageType Type { get; set; }
		public string? Content { get; set; }
		public string? UserId { get; set; }
		public string? UserName { get; set; }
		public DateTime Timestamp { get; set; }
	}
}
