using Google.Apis.YouTube.v3.Data;
using System.Threading.Tasks;

namespace GameController.YouTubeService.Services
{
	public interface IYouTubeService
	{

		Task<string> GetLiveChatIdAsync(string videoId);
		Task<LiveChatMessageListResponse> GetLiveChatMessagesAsync(string liveChatId, string? pageToken = null);
		Task<LiveChatMessage> SendLiveChatMessageAsync(string liveChatId, string messageText);
		Task<Google.Apis.YouTube.v3.Data.Channel> GetChannelDetailsAsync(string channelId);


	}
}
