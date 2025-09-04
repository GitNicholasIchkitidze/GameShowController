using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameController.Shared.Models
{
	public class OAuthTokenModels
	{
		public string AccessToken { get; set; }
		public string RefreshToken { get; set; }
		public DateTime ExpirationDate { get; set; }
	}
}
