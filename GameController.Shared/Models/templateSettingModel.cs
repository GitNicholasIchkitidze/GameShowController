using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameController.UI.Model
{
	public class templateSettingModel
	{
		public string TemplateType { get; set; }
		public string TemplateName { get; set; }
		public string TemplateUrl { get; set; }		
		public int Channel { get; set; }		
		public int Layer { get; set; }
		public int LayerCg { get; set; }
		public string ServerIP { get; set; }

		public Boolean IsRegistered { get; set; }

		// Add a constructor that takes all 6 arguments
		public templateSettingModel(string templateType, string templateName, string templateUrl, int channel,  int layer, int layerCg, string serverIP)
		{
			TemplateType = templateType;
			TemplateName = templateName;
			ServerIP = serverIP;
			Channel = channel;
			TemplateUrl = templateUrl;
			Layer = layer;
			LayerCg = layerCg;
		}

		// Optionally, keep the parameterless constructor for serialization/binding
		public templateSettingModel() { }
	}

}
