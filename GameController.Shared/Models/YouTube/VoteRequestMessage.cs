using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameController.Shared.Models.YouTube
{
	public class VoteRequestMessage
	{
		public bool IsVotingActive { get; set; }
		public TimeSpan? Duration { get; set; }
		public DateTime? EndTime { get; set; }
		public string? QuestionId { get; set; }
		public int? VotingModeDurationMinutes { get; set; } // Nullable int to handle indefinite voting.

	}
}
