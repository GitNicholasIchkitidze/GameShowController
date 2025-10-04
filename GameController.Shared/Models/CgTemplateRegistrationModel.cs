using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameController.Shared.Models
{
	public class TemplateRegistrationModel
	{
		public string Type { get; set; } = string.Empty;
		public string TemplateName { get; set; } = string.Empty;


		public bool? IsRegistered { get; set; } 
		public string ConnectionId { get; set; } = string.Empty;
	}
}
