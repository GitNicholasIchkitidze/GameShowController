using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameController.Shared.Models.FaceBook
{
	public class VoteRecord
	{
		public int Id { get; set; }
		public string UserName { get; set; } = string.Empty;
		public string Message { get; set; } = string.Empty;
		public DateTime CreatedAt { get; set; }
	}
}
