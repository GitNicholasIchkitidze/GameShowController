using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameController.Shared.Models
{
	public class ClientConfiguration
	{
		public required string clientName { get; set; }
		public required List<string?> ip { get; set; }
		public required string clientType { get; set; }
	}
}
