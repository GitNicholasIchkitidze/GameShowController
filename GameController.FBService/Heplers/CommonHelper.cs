// Heplers/RedisTtlProvider.cs
using GameController.FBService.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace GameController.FBService.Heplers
{
    
    public interface ICommonHelper
    {
        
        Boolean IsThisMe(string id);
        string getCandidatePhone(string VoteName);

    }

	public class CommonHelper : ICommonHelper
    {
		
        private readonly IConfiguration _configuration;
        private readonly List<FBClientConfiguration> _registeredClients;
        public CommonHelper(IConfiguration configuration)
		{
            _configuration = configuration;
            _registeredClients = _configuration.GetSection("RegisteredClients").Get<List<FBClientConfiguration>>() ?? new List<FBClientConfiguration>();
        }

        public bool IsThisMe(string id)
        {
            return id == "7176465872405704";
        }


        public string getCandidatePhone(string VoteName)
        {
            var result = _registeredClients.Where(x => x.clientName == VoteName).FirstOrDefault().phone;
            return result;
        }

    }
}
