namespace GameController.FBService.Models
{
	public class Vote
	{
		public required string Id { get; set; }
		public required string UserId { get; set; }
		public required string CandidateName { get; set; }
		public DateTime Timestamp { get; set; }

		public required string Message { get; set; }
	}
}
