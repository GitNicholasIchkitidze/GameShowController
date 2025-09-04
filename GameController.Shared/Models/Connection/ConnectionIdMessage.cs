using GameController.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameController.Shared.Models.Connection
{
	public class ConnectionIdMessage
	{
		/// <summary>
		/// The unique ID of the client connection.
		/// </summary>
		public string ConnectionId { get; set; } = string.Empty;

		/// <summary>
		/// The name of the service (e.g., "YouTubeChatService").
		/// </summary>
		public string ServiceName { get; set; } = string.Empty;
		public ClientTypes clientTypes { get; set; } = ClientTypes.Unknown;
	}
}
