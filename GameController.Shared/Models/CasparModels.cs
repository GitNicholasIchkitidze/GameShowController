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

        public required CasparCGTemplate tPs1 { get; set; }
        public required CasparCGVideoTemplate QuestionVideo { get; set; }
	}


	public class CasparCGTemplate
	{

		public required string TemplateName { get; set; }
		public required string TemplateUrl { get; set; }
		public int Channel { get; set; }
		public int Layer { get; set; }
		public int LayerCg { get; set; }

		public string? ServerIp { get; set; }
		public CasparCGTemplate(string templateName, string templateUrl, int channel, int layer, int layerCg)
		{
			TemplateName = templateName;
			TemplateUrl = templateUrl;
			Channel = channel;
			Layer = layer;
			LayerCg = layerCg;
			ServerIp = new Uri(templateUrl).Host;
		}
	}


	public class CasparCGVideoTemplate
	{
		public required string TemplateName { get; set; }
		public required string TemplateUrl { get; set; }
		public int Channel { get; set; }
		public int Layer { get; set; }
		public int LayerCg { get; set; }
		public string? ServerIp { get; set; }

		public CasparCGVideoTemplate(string templateName, string templateUrl, int channel, int layer, int layerCg, string serverIp)
		{
			TemplateName = templateName;
			TemplateUrl = templateUrl;
			Channel = channel;
			Layer = layer;
			LayerCg = layerCg;
			ServerIp = serverIp;
		}
	}

}
