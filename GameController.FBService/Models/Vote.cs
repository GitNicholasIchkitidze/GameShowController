using GameController.FBService.AntiBotServices;

namespace GameController.FBService.Models
{
	public class Vote
	{
		public required string Id { get; set; }
		public required string? MSGId { get; set; }
		public required string? MSGRecipient { get; set; }
		public required string UserId { get; set; }
		
		public required string CandidateName { get; set; }
		public string? CandidatePhone { get; set; }
		public DateTime Timestamp { get; set; }

		public required string? Message { get; set; }
		public string? UserName { get; set; }

		public bool? IsSuspicious { get; set; }
		public int? RiskScore { get; set; }
		public string[]? Flags { get; set; } = Array.Empty<string>();
		public bool? ShouldBlock { get; set; }


	}

	public class BanAccount
    {
        public required string Id { get; set; }
        public required string UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserProvider { get; set; }
        public bool IsSuspicious { get; set; }
		public int RiskScore { get; set; }
		public string[] Flags { get; set; } = Array.Empty<string>();
		public bool ShouldBlock { get; set; }
        public bool Banned { get; set; }
        public string? BannedMsg { get; set; }
        public DateTime? BannedDate { get; set; }
    }

    public class ClickerDecision
    {
        public bool IsSuspicious { get; set; }
        public int RiskScore { get; set; }
        public string[] Flags { get; set; } = Array.Empty<string>();
        public bool ShouldBlock { get; set; }
        public bool ShouldAskConfirmation { get; set; }
    }

    public class VotingOptions
	{
		public int CooldownMinutes { get; set; } = 5;
		public int NeedForVoteMinutes { get; set; } = 10;

        public string VoteSessionPrefix { get; set; } 
    }
}
