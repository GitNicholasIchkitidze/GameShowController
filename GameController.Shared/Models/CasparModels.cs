using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameController.Shared.Models
{
	public class CasparCGSettings
	{
		public required string ServerIp { get; set; }
		public int ServerPort { get; set; }


		public required CasparCGTemplate QuestionFull { get; set; }
		public required CasparCGTemplate QuestionLower { get; set; }
		public required CasparCGTemplate LeaderBoard { get; set; }
		public required CasparCGTemplate CountDown { get; set; }
		public required CasparCGTemplate YTVote { get; set; }
		public required CasparCGVideoTemplate QuestionVideo { get; set; }
	}


	public class CasparCGTemplate
	{
		public required string TemplateName { get; set; }
		public int Channel { get; set; }
		public int Layer { get; set; }
		public int LayerCg { get; set; }
	}
	public class CasparCGVideoTemplate
	{
		public required string TemplateName { get; set; }
		public int Channel { get; set; }
		public int Layer { get; set; }
		public int LayerCg { get; set; }
	}

}
