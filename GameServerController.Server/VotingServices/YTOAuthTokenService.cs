namespace GameController.Server.VotingServices
{
    public class YTOAuthTokenService : IYTOAuthTokenService
    {
        private string? _accessToken;
        private string? _refreshToken;
        private readonly TaskCompletionSource<string> _accessTokenTcs = new TaskCompletionSource<string>();


        public YTOAuthTokenService()
        {
            // აქ დაემატება ტოკენის ინიციალიზაციის ლოგიკა.
        }

        public async Task<string> GetAccessTokenAsync()
        {
            // აქ დაემატება ტოკენის ვადის შემოწმება და განახლება.
            // ამ ეტაპზე უბრალოდ დააბრუნეთ მიმდინარე ტოკენი.
            // თუ ტოკენი უკვე არსებობს, დააბრუნეთ ის დაუყოვნებლივ დასრულებული Task-ის სახით.
            return _accessToken != null ? _accessToken : await _accessTokenTcs.Task;
        }

        public async Task SaveTokensAsync(string accessToken, string refreshToken)
        {
            // აქ დაემატება ტოკენების შენახვის ლოგიკა.
            _accessToken = accessToken;
            _refreshToken = refreshToken;

            _ = _accessTokenTcs.TrySetResult(accessToken);

            await Task.CompletedTask;
        }
    }
}
