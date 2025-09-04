using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameController.Shared.Models.YouTube
{
	public class VoteResult
	{
		public string? Answer { get; set; }
		public int Count { get; set; }
		public double Percentage { get; set; }
	}
}
