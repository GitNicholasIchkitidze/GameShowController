namespace GameController.Server.VotingServices
{
    public interface IYTOAuthTokenService
    {
        Task<string> GetAccessTokenAsync();
        Task SaveTokensAsync(string accessToken, string refreshToken);
    }
}
