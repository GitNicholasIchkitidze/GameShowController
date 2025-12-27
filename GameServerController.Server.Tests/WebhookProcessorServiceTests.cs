using FluentAssertions;
using GameController.FBService.Services;
using GameController.FBService.Tests.Fakes;
using GameServerController.Server.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameServerController.Server.Tests
{
    public class WebhookProcessorServiceTests
    {
        [Fact]
        public async Task ProcessAsync_WhenVoteStartMessage_ShouldSendFacebookReply()
        {
            // ---------------- CONFIGURATION ----------------
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["pageAccessToken"] = "TEST_TOKEN",
                    ["voteStartFlag"] = "START",
                    ["imageFolderPath"] = "C:\\Temp",
                    ["voteMinuteRange"] = "5",
                    ["RegisteredClients:0:ClientId"] = "TEST_CLIENT",
                    ["RegisteredClients:0:Name"] = "Test Client"
                })
                .Build();

            // ---------------- DB CONTEXT ----------------
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("WebhookProcessorTests")
                .Options;

            var dbContext = new ApplicationDbContext(options);

            // ---------------- FAKES ----------------
            var httpHandler = new CapturingHttpMessageHandler();
            var httpFactory = new FakeHttpClientFactory(httpHandler);

            var cacheService = new FakeCacheService();
            var rateLimitingService = new AllowAllRateLimitingService();
            var idempotencyService = new AllowAllIdempotencyService();
            var varsKeeper = new InMemoryGlobalVarsKeeper();

            await varsKeeper.SetValueAsync("fb_listening_active", true);

            // ---------------- SUT ----------------
            var processor = new WebhookProcessorService(
                NullLogger<WebhookProcessorService>.Instance,
                dbContext,
                cacheService,
                rateLimitingService,
                configuration,
                httpFactory,
                idempotencyService,
                varsKeeper
            );

            var message = new IncomingFacebookMessage
            {
                SenderId = "USER_PSID_1",
                MessageId = "m_123",
                MessageText = "START"
            };

            // ---------------- ACT ----------------
            await processor.ProcessAsync(message);

            // ---------------- ASSERT ----------------
            httpHandler.Requests.Should().NotBeEmpty("Processor should send FB reply");

            var request = httpHandler.Requests.Single();
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsoluteUri.Should().Contain("/me/messages");

            var body = await request.Content!.ReadAsStringAsync();
            body.Should().Contain("USER_PSID_1");
        }
    }

}
