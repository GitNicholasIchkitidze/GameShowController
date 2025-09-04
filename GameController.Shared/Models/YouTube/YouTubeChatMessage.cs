using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameController.Shared.Models.YouTube
{
	public class YouTubeChatMessage
	{
		public string? Id { get; set; }
		public string? AuthorChannelId { get; set; }
		public string? UserName { get; set; }
		public string? MessageText { get; set; }
		//public DateTime? PublishedAt { get; set; }
		public string? PublishedAt { get; set; }
		public string? ProfileImageUrl { get; set; }
	}
}
