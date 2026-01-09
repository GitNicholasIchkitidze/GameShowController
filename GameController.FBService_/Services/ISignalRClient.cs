namespace GameController.FBService.Services
{
	public interface ISignalRClient
	{
		Task SendVoteToHub(string candidateName);
		Task ConnectWithRetryAsync();
	}
}
