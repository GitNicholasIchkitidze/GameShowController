using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameController.Shared.Models.Connection
{
	public class MessageToYTManager
	{
		public string SenderConnectionId { get; set; } = string.Empty;
		public string SenderName { get; set; } = string.Empty;


		public string MessageText { get; set; } = string.Empty;
	}
}
