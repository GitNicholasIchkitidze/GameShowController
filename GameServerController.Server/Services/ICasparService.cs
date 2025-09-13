using GameController.Shared.Models;

namespace GameController.Server.Services
{
	public interface ICasparService
	{
		Task PlayTemplate(int channel, int layer, string templateName, object data);
		Task<OperationResult> LoadTemplate(string templateName, int channel, int layer, int layerCg, bool autoPlay, object? data);

		Task UnLoadTemplate(string templateName);
		Task ClearChannel(int channel);
		Task PlayClip(int channel, int layer, string templateName);
		Task UpdateTemplate(int channel, int layer, int layerCg, bool autoPlay, object? data);
		Task InvokeTemplate(int channel, int layer, int layerCg, string methodeName, object? data);
		Task StopTemplate(int channel, int layer);
		Task<OperationResult> SendCommand(string cmd);

		string CreateTemplateData(object data);

	}
}
