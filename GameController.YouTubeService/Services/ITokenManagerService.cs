using Google.Apis.Auth.OAuth2;

namespace GameController.YouTubeService.Services
{
	public interface ITokenManagerService
	{
		/// <summary>
		/// Initializes the service by loading or acquiring user credentials.
		/// </summary>
		Task InitializeAsync();

		/// <summary>
		/// Gets the user credential, refreshing the access token if needed.
		/// </summary>
		Task<UserCredential> GetCredentialAsync();
	}
}
