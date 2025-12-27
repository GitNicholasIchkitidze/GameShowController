using System.Net.Http;

namespace GameController.FBService.Tests.Fakes;

public class FakeHttpClientFactory : IHttpClientFactory
{
    private readonly HttpClient _client;

    public FakeHttpClientFactory(CapturingHttpMessageHandler handler)
    {
        _client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://graph.facebook.com/")
        };
    }

    public HttpClient CreateClient(string name = "") => _client;
}
