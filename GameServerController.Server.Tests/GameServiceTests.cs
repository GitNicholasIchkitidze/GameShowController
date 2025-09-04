using Xunit;
using Moq;
using GameController.Server.Services;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using GameController.Server.VotingServices;

namespace GameServerController.Server.Tests
{
	public class GameServiceTests
	{
		[Fact]
		public async Task GetLiveChatIdAsync_ReturnsCorrectId_WhenSuccessful()
		{
			// Arrange
			var mockHttpClient = new Mock<HttpClient>();
			var mockLogger = new Mock<ILogger<YouTubeChatService>>();

			//var service = new YouTubeChatService(mockHttpClient.Object, null, null);
			//string videoId = "testVideoId";

			// ამ ეტაპზე დაგვჭირდება HTTP მოთხოვნის Mock-ირება, რათა დავიბრუნოთ კონკრეტული JSON პასუხი.
			// ...

			// Act
			// string liveChatId = await service.GetLiveChatIdAsync(videoId);

			// Assert
			// Assert.Equal("expectedLiveChatId", liveChatId);
		}
	}
}