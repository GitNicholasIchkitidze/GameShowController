using System.Net.WebSockets;

namespace GameController.Server.Services
{
    public interface ICasparCGWsService
    {
        void AddConnection(WebSocket socket);
        Task ReceiveRegistrationAsync(WebSocket socket, string connectionId);
        Task<bool> SendDataToTemplateAsync(string templateType, object data);
        Task SendQuestionDataAsync(object data);
    }
}
