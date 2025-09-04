using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameController.Shared.Models
{
	public class QuestionModel
	{
		public required string QuestionID { get; set; }
		public required string Question { get; set; }
		public string? QuestionImage { get; set; }
		public required List<string> Answers { get; set; }
		public required string CorrectAnswer { get; set; }

	}



}
