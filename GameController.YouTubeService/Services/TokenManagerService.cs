using Google.Apis.Auth.OAuth2;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GameController.YouTubeService.Services
{
	public class TokenManagerService : ITokenManagerService
	{
		private readonly IConfiguration _configuration;
		private UserCredential _credential;

		public TokenManagerService(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public async Task InitializeAsync()
		{
			try
			{
				// The client_secrets.json file should be in the same directory as the executable.
				string secretsFile = "client_secrets.json";
				if (!File.Exists(secretsFile))
				{
					throw new FileNotFoundException($"The '{secretsFile}' file was not found. Please ensure it is in the application's root directory.");
				}

				// Load or acquire credentials
				_credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
					GoogleClientSecrets.FromFile(secretsFile).Secrets,
					new[] { Google.Apis.YouTube.v3.YouTubeService.Scope.YoutubeForceSsl }, // FIX: Use fully qualified name
					"user",
					CancellationToken.None);

				// Save tokens for future use to avoid re-authorization
				string tokenJson = JsonSerializer.Serialize(_credential.Token);
				await File.WriteAllTextAsync("token.json", tokenJson);
			}
			catch (Exception ex)
			{
				// Better logging required here
				throw new InvalidOperationException("Failed to initialize or refresh token.", ex);
			}
		}

		public async Task<UserCredential> GetCredentialAsync()
		{
			// The UserCredential object automatically handles token refreshing on its own,
			// but checking the expiration time adds an extra layer of safety.
			//if (_credential.Token.IsExpired(TimeSpan.FromMinutes(5)))
			//{
			//	await _credential.RefreshTokenAsync(CancellationToken.None);
			//}
			await Task.CompletedTask; // Ensures the method is truly asynchronous
			return _credential;
		}
	}
}