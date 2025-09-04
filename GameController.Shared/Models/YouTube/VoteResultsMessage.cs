using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameController.Shared.Models.YouTube
{
	public class VoteResultsMessage
	{
		public string? QuestionId { get; set; }
		public string? QuestionText { get; set; }
		public string? MessageText { get; set; }
		public List<VoteResult> Results { get; set; } = new List<VoteResult>();
		public int? TotalVotes { get; set; }

		public string? VoterName { get; set; }
		public string? ProfileImageUrl { get; set; }
	}
}
