namespace GameController.FBService.Models
{
	public class Vote
	{
		public required string Id { get; set; }
		public required string? MSGId { get; set; }
		public required string UserId { get; set; }
		
		public required string CandidateName { get; set; }
		public string? CandidatePhone { get; set; }
		public DateTime Timestamp { get; set; }

		public required string? Message { get; set; }
		public string? UserName { get; set; }
	}
}
