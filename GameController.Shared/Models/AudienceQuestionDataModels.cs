using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameController.Shared.Models
{
	public class AudienceQuestionData
	{
		public int QuestionId { get; set; }
		public string QuestionText { get; set; }
		public ConcurrentDictionary<string, int> AnswerCounts { get; set; } = new ConcurrentDictionary<string, int>();
		public Dictionary<string, double> AnswerPercentages { get; set; } = new Dictionary<string, double>();
		public int TotalVotes { get; set; }
	}
}
