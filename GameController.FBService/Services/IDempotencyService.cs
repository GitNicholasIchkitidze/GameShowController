namespace GameController.FBService.Services
{
	public interface IDempotencyService
	{
		Task<bool> IsDuplicateAsync(string messageId);
	}
}
