using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameController.Shared.Models
{
	public class AudienceMember
	{
		public string AuthorChannelId { get; set; }
		public string AuthorName { get; set; }
		public string Answer { get; set; }

		/// <summary>
		/// პლატფორმა, საიდანაც პასუხი გაიცა (მაგალითად: "YouTube", "Facebook").
		/// </summary>
		public string Platform { get; set; }
	}
}
