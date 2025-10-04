using GameController.Shared.Enums;
using GameController.Shared.Models;
using GameController.Shared.Models.YouTube;
using GameController.UI.Model;

namespace GameController.Server.Services
{
	public interface ICasparManager
	{
		Task<OperationResult> LoadTemplateAsync(CGTemplateEnums templateType);

		Task<OperationResult> ToggleTitle(TitleDataModel title, bool show);
        Task<OperationResult> UpdatecgSettingsMap(CGTemplateEnums templateType,
			Dictionary<CGTemplateEnums, templateSettingModel> cgSettingsMap);
		Task<OperationResult> EnsureTemplateLoadedAsync(string templateName, int channel, int layer);
		Task<OperationResult> PlayClipAsync(int channel, int layer, string clipName);
		Task<OperationResult> ClearPlayClipAsync(CGTemplateEnums templateType);

		Task<OperationResult> ClearChannelAsync(CGTemplateEnums templateType);
		Task<OperationResult> ClearChannelLayerAsync(CGTemplateEnums templateType);
		Task<OperationResult> UpdateQuestionTemplateAsync(string templateType, QuestionModel question);
		Task<OperationResult> ShowCorrectAnswerAsync(string templateType, int correctAnswerIndex);
		Task<OperationResult> StartCountDownAsync(string templateType, int duration, CountDownStopMode action, long endTimestamp);
		Task<OperationResult> UpdateYTVoteTemplateAsync(string templateType, object message);

		Task<OperationResult> CGSWShowFinalResultsAsync();

		Task<OperationResult> CGSWToggleLeaderBoardAsync(bool isLeaderBoardActive, bool isVisible);
		
		Task CGSWUpdateLeaderBoardAsync(bool isLeaderBoardActive, bool isFinal = false);
		Task CGSWStoreFinalResultsAsync();
		Task CGWSYTVoteAsync(string templateType, VoteResultsMessage message);
		Task CGWSUpdateInQuestionShowCorrectTemplateDataAsync(string templateType, object message);
		Task CGWSClearChannelLayerAsync(CGTemplateEnums templateType);
	}
}
