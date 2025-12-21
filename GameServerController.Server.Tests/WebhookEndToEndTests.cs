using FluentAssertions;
using GameController.FBService.Heplers;
using GameController.FBService.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using Xunit;

namespace GameController.FBService.Tests;

public class WebhookEndToEndTests : IClassFixture<FbServiceFactory>
{
    private readonly FbServiceFactory _factory;
    private readonly HttpClient _client;

    public WebhookEndToEndTests(FbServiceFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task FacebookWebhook_Message_GoesThroughPipeline_AndSendsReply()
    {

        var vars = _factory.Services.GetRequiredService<IGlobalVarsKeeper>();
        await vars.SetValueAsync("fb_listening_active", true);

        // -------- Arrange --------
        var payload = """
        {
          "object": "page",
          "entry": [{
            "messaging": [{
              "sender": { "id": "USER_PSID_1" },
              "message": {
                "mid": "m_123",
                "text": "A"
              }
            }]
          }]
        }
        """;

        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        // -------- Act --------
        var response = await _client.PostAsync("/api/FacebookWebhooks", content);

        // -------- Assert (HTTP) --------
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // -------- Assert (async background processing) --------
        var handler = _factory.Services.GetRequiredService<CapturingHttpMessageHandler>();

        await EventuallyAsync(() =>
        {
            handler.Requests.Should().NotBeEmpty();
            return Task.CompletedTask;
        });

        // -------- Assert (Facebook Send API call) --------
        //var fbCall = handler.Requests
        //    .First(r => r.Uri.AbsoluteUri.Contains("/me/messages"));
        //fbCall.Method.Should().Be(HttpMethod.Post);
        //fbCall.Body.Should().Contain("USER_PSID_1");

        handler.Requests.Should().NotBeEmpty("No outbound requests were captured");

        var allUrls = string.Join("\n", handler.Requests.Select(r => r.Uri.AbsoluteUri));
        allUrls.Should().Contain("/me/messages", $"Captured URLs:\n{allUrls}");


        

        // სურვილის შემთხვევაში:
        // fbCall.Body.Should().Contain("A");
        // fbCall.Body.Should().Contain("მიღებულია");
    }

    private static async Task EventuallyAsync(
        Func<Task> assertion,
        int timeoutMs = 3000,
        int stepMs = 50)
    {
        var start = DateTime.UtcNow;
        Exception? last = null;

        while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
        {
            try
            {
                await assertion();
                return;
            }
            catch (Exception ex)
            {
                last = ex;
                await Task.Delay(stepMs);
            }
        }

        throw last ?? new Exception("Condition not met in time");
    }
}
