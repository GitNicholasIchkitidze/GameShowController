using System.Net;
using System.Text;

namespace GameController.FBService.Tests.Fakes;

public record CapturedRequest(HttpMethod Method, Uri Uri, string Body);

public class CapturingHttpMessageHandler : HttpMessageHandler
{
    public List<CapturedRequest> Requests { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);

        Requests.Add(new CapturedRequest(
            request.Method,
            request.RequestUri!,
            body
        ));

        // ვაბრუნებთ თითქოს FB-მა მიიღო
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"result\":\"ok\"}", Encoding.UTF8, "application/json")
        };
    }
}
