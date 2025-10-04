using GameController.Server.Extensions;
using GameController.Server.Services;
using GameController.Shared.Enums;
using GameController.Shared.Models;
using GameController.Shared.Models.Connection;
using GameController.Shared.Models.YouTube;
using GameController.UI.Model;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace GameController.Server.Services
{
	public class CasparManager : ICasparManager
	{
		private readonly ILogger<CasparManager> _logger;
		private readonly ICasparService _caspar;
		private readonly CasparCGWsService _casparCGWsService;
		private readonly IGameService _gameService;
		private readonly CasparCGSettings? _cgSettings;
		private readonly ConcurrentDictionary<string, bool> _isTemplateLoaded = new();
		//private static Dictionary<CGTemplateEnums, (string serverIP, int channel, string templateName, int layer, int layerCg)> _cgSettingsMap = new();
		public static Dictionary<CGTemplateEnums, templateSettingModel> _cgSettingsMap = new();

		public CasparManager(
			ILogger<CasparManager> logger,
			ICasparService caspar,
			CasparCGWsService casparCGWsService,
			IGameService gameService,
			IConfiguration configuration)
		{
			_logger = logger;
			_caspar = caspar;
			_casparCGWsService = casparCGWsService;
			_gameService = gameService;
			_cgSettings = configuration.GetSection("CG").Get<CasparCGSettings>();

			if (_cgSettings != null)
			{
				_cgSettingsMap[CGTemplateEnums.QuestionFull] = new templateSettingModel(
					CGTemplateEnums.QuestionFull.ToString(),
					_cgSettings.QuestionFull.TemplateName,
					_cgSettings.QuestionFull.TemplateUrl,					
					_cgSettings.QuestionFull.Channel,					
					_cgSettings.QuestionFull.Layer,
					_cgSettings.QuestionFull.LayerCg,
					_cgSettings.QuestionFull.ServerIp ?? string.Empty
				);
				_cgSettingsMap[CGTemplateEnums.QuestionLower] = new templateSettingModel(
					CGTemplateEnums.QuestionLower.ToString(),
					_cgSettings.QuestionLower.TemplateName,
					_cgSettings.QuestionLower.TemplateUrl,
					_cgSettings.QuestionLower.Channel,
					_cgSettings.QuestionLower.Layer,
					_cgSettings.QuestionLower.LayerCg,
					_cgSettings.QuestionLower.ServerIp ?? string.Empty
				);
				_cgSettingsMap[CGTemplateEnums.CountDown] = new templateSettingModel(
					CGTemplateEnums.CountDown.ToString(),
					_cgSettings.CountDown.TemplateName,
					_cgSettings.CountDown.TemplateUrl,
					_cgSettings.CountDown.Channel,
					_cgSettings.CountDown.Layer,
					_cgSettings.CountDown.LayerCg,
					_cgSettings.CountDown.ServerIp ?? string.Empty
				);
				_cgSettingsMap[CGTemplateEnums.LeaderBoard] = new templateSettingModel(
					CGTemplateEnums.LeaderBoard.ToString(),
					_cgSettings.LeaderBoard.TemplateName,
					_cgSettings.LeaderBoard.TemplateUrl,
					_cgSettings.LeaderBoard.Channel,
					_cgSettings.LeaderBoard.Layer,
					_cgSettings.LeaderBoard.LayerCg,
					_cgSettings.LeaderBoard.ServerIp ?? string.Empty
				);
				_cgSettingsMap[CGTemplateEnums.YTVote] = new templateSettingModel(
					CGTemplateEnums.YTVote.ToString(),
					_cgSettings.YTVote.TemplateName,
					_cgSettings.YTVote.TemplateUrl,
					_cgSettings.YTVote.Channel,
					_cgSettings.YTVote.Layer,
					_cgSettings.YTVote.LayerCg,
					_cgSettings.YTVote.ServerIp ?? string.Empty
				);
				_cgSettingsMap[CGTemplateEnums.QuestionVideo] = new templateSettingModel(
					CGTemplateEnums.QuestionVideo.ToString(),
					_cgSettings.QuestionVideo.TemplateName,
					_cgSettings.QuestionVideo.TemplateUrl,
					_cgSettings.QuestionVideo.Channel,
					_cgSettings.QuestionVideo.Layer,
					_cgSettings.QuestionVideo.LayerCg,
					_cgSettings.QuestionVideo.ServerIp ?? string.Empty
				);
                _cgSettingsMap[CGTemplateEnums.tPs1] = new templateSettingModel(
                    CGTemplateEnums.tPs1.ToString(),
                    _cgSettings.tPs1.TemplateName,
                    _cgSettings.tPs1.TemplateUrl,
                    _cgSettings.tPs1.Channel,
                    _cgSettings.tPs1.Layer,
                    _cgSettings.tPs1.LayerCg,
                    _cgSettings.tPs1.ServerIp ?? string.Empty
                );
            }

			_logger.LogInformationWithCaller("CasparCGManager instance created");
			//_logger.LogInformation(new EventId(1000, "CasparManagerConstructor"), $"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} CasparCGManager instance created.");
		}

		

		public async Task<OperationResult> UpdatecgSettingsMap(CGTemplateEnums templateType,
			Dictionary<CGTemplateEnums, templateSettingModel> cgSettingsMap)
		{
			await Task.Yield(); // Ensures the method is truly asynchronous
			_cgSettingsMap = cgSettingsMap;
			_logger.LogInformationWithCaller("CG Settings Updated");
			//await Task.CompletedTask;

			UpdateCGAppSettings();

			return new OperationResult(_cgSettingsMap == cgSettingsMap);
		}

		


		private void UpdateCGAppSettings()
		{
			try
			{
				var appSettingsPath = "appsettings.json";

				// წაიკითხე არსებული JSON ფაილი Newtonsoft.Json-ის გამოყენებით
				var json = File.ReadAllText(appSettingsPath);
				var jObject = JObject.Parse(json);

				// განაახლე CG სექცია
				var cgSection = jObject["CG"] as JObject;
				if (cgSection != null)
				{
					foreach (var kvp in _cgSettingsMap)
					{
						var templateName = kvp.Key.ToString();
						var setting = kvp.Value;

						if (setting == null) continue;

						// თუ template არსებობს CG სექციაში, განაახლე იგი
						if (cgSection[templateName] is JObject templateObject)
						{
							templateObject["TemplateName"] = setting.TemplateName ?? "";
							templateObject["TemplateUrl"] = setting.TemplateUrl ?? "";
							templateObject["Channel"] = setting.Channel;
							
							templateObject["Layer"] = setting.Layer;
							templateObject["LayerCg"] = setting.LayerCg;
							templateObject["ServerIp"] = setting.ServerIP ?? "";
						}
						else
						{
							// თუ არ არსებობს, შექმენი ახალი
							var newTemplate = new JObject
							{
								["TemplateName"] = setting.TemplateType ?? "",
								["TemplateUrl"] = setting.TemplateUrl ?? "",								
								["Channel"] = setting.Channel,								
								["Layer"] = setting.Layer,
								["LayerCg"] = setting.LayerCg,
								["ServerIp"] = setting.ServerIP ?? ""
							};
							cgSection[templateName] = newTemplate;
						}
					}
				}

				// ჩაწერე უკან ფაილში ფორმატირებით
				var updatedJson = jObject.ToString(Formatting.Indented);
				File.WriteAllText(appSettingsPath, updatedJson);
			}
			catch (Exception ex)
			{
				// დალოგე შეცდომა
				Console.WriteLine($"Settings Saving Error: {ex}");
				throw;
			}
		}

		private void WriteCGSection(Utf8JsonWriter writer, JsonElement existingCgElement)
		{
			writer.WriteStartObject();

			// 1. ჯერ დაწერე ყველა არსებული property რომელიც არ არის template
			foreach (var property in existingCgElement.EnumerateObject())
			{
				// თუ property არის template (_cgSettingsMap-ში არსებობს), გამოტოვე იგი
				if (!_cgSettingsMap.Keys.Any(k => k.ToString() == property.Name))
				{
					property.WriteTo(writer);
				}
			}

			// 2. დაწერე ყველა template _cgSettingsMap-დან
			foreach (var kvp in _cgSettingsMap)
			{
				if (kvp.Value == null) continue;

				writer.WritePropertyName(kvp.Key.ToString());
				writer.WriteStartObject();
				writer.WriteString("ServerIp", kvp.Value.ServerIP ?? "");
				writer.WriteNumber("Channel", kvp.Value.Channel);
				writer.WriteString("TemplateUrl", kvp.Value.TemplateUrl ?? "");
				writer.WriteNumber("Layer", kvp.Value.Layer);
				writer.WriteNumber("LayerCg", kvp.Value.LayerCg);
				writer.WriteEndObject();
			}

			writer.WriteEndObject();
		}

        public async Task<OperationResult> ToggleTitle(TitleDataModel title, bool show)
		{
			try
			{
				var data = new
				{
					type = show ? "show" : "hide",
					Status = title.Status,
					BreakingNews = title.BreakingNews,
					Headline = title.Headline,
					SecondLine = title.SecondLine
				};
				await _casparCGWsService.SendDataToTemplateAsync("tPs1", data);
				_logger.LogInformationWithCaller($"Toggled title to {(show ? "show" : "hide")} with headline: {title.Headline}");
				return new OperationResult(true);
			}
			catch (Exception ex)
			{
				_logger.LogErrorWithCaller($"Error toggling title: {ex.Message}");
				return new OperationResult(false, ex.Message);
            }
        }


        public async Task<OperationResult> LoadTemplateAsync(CGTemplateEnums templateType)
		{
			var cgTemplateSetting = _cgSettingsMap.GetValueOrDefault(templateType);
			if (string.IsNullOrEmpty(cgTemplateSetting?.TemplateUrl))
			{
				_logger.LogErrorWithCaller($"Template name not found for {templateType}.");
				return new OperationResult(false, "Template name not configured.");
			}

			return await EnsureTemplateLoadedAsync(cgTemplateSetting.TemplateUrl, cgTemplateSetting.Channel, cgTemplateSetting.Layer);
		}

		public async Task<OperationResult> EnsureTemplateLoadedAsync(string templateName, int channel, int layer)
		{
			var key = $"{channel}-{layer}";
			const int maxRetries = 3;

			if (!_isTemplateLoaded.ContainsKey(key))
			{
				int retryCount = 0;
				OperationResult res = new(true);

				while (retryCount < maxRetries)
				{
					res = await _caspar.LoadTemplate(templateName, channel, layer, 1, false, null);
					if (res.Result)
					{
						_isTemplateLoaded[key] = true;
						_logger.LogInformationWithCaller($"Template '{templateName}' loaded successfully on {channel}:{layer}.");
						return res;
					}

					retryCount++;
					_logger.LogWarning($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Template load failed (attempt {retryCount}/{maxRetries}): {res.Message}");
					if (retryCount < maxRetries)
						await Task.Delay(1000);
				}

				_logger.LogError($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Final failure loading template '{templateName}' after {maxRetries} attempts: {res.Message}");
				return res;
			}

			return new OperationResult(true, "Template already loaded.");
		}

		public async Task<OperationResult> ClearChannelAsync(CGTemplateEnums templateType)
		{
			try
			{
				var _cgTemTemplateSetting = _cgSettingsMap.GetValueOrDefault(templateType);

				
				if (_cgTemTemplateSetting?.Channel > 0)
				{
					await _caspar.ClearChannel(_cgTemTemplateSetting.Channel);
					_logger.LogInformationWithCaller($"Cleared channel {_cgTemTemplateSetting?.Channel} for {templateType}.");
					return new OperationResult(true);
				}
				return new OperationResult(false, "Invalid channel.");
			}
			catch (Exception ex)
			{
				_logger.LogError( $"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Error clearing channel for {templateType} {ex.Message}.");
				return new OperationResult(false, ex.Message);
			}
		}

		public async Task<OperationResult> ClearChannelLayerAsync(CGTemplateEnums templateType)
		{
			try
			{
				var data = new { type = "clear_content", Question = "", QuestionImage = "", Answers = "" };
				await _casparCGWsService.SendDataToTemplateAsync(templateType.ToString(), data);
				_logger.LogInformationWithCaller($"Cleared layer for {templateType}.");
				return new OperationResult(true);
			}
			catch (Exception ex)
			{
				_logger.LogError($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Error clearing layer for {templateType} {ex}.");
				return new OperationResult(false, ex.Message);
			}
		}

		public async Task<OperationResult> UpdateQuestionTemplateAsync(string templateType, QuestionModel question)
		{
			try
			{
				var data = new
				{
					type = "show_question",
					Question = question.Question,
					QuestionImage = question.QuestionImage,
					Answers = question.Answers
				};
				await _casparCGWsService.SendDataToTemplateAsync(templateType, data);
				_logger.LogInformationWithCaller($"Updated question template '{templateType}' for question: {question.Question}");
				return new OperationResult(true);
			}
			catch (Exception ex)
			{
				_logger.LogError($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Error updating question template '{templateType}' {ex}.");
				return new OperationResult(false, ex.Message);
			}
		}

		public async Task<OperationResult> UpdateYTVoteTemplateAsync(string templateType, object message)
		{
			try
			{
				await _casparCGWsService.SendDataToTemplateAsync(templateType, message);
				_logger.LogInformationWithCaller($"Updated YT Vote template '{templateType}'.");
				return new OperationResult(true);
			}
			catch (Exception ex)
			{
				_logger.LogError($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Error updating YT Vote template '{templateType}' {ex}.");
				return new OperationResult(false, ex.Message);
			}
		}

		public async Task<OperationResult> ShowCorrectAnswerAsync(string templateType, int correctAnswerIndex)
		{
			try
			{
				var message = new { type = "show_answer", correctAnswerIndex };
				await _casparCGWsService.SendDataToTemplateAsync(templateType, message);
				_logger.LogInformationWithCaller($"Showed correct answer {correctAnswerIndex} for '{templateType}'.");
				return new OperationResult(true);
			}
			catch (Exception ex)
			{
				_logger.LogError($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Error showing correct answer for '{templateType}' {ex}.");
				return new OperationResult(false, ex.Message);
			}
		}

		public async Task<OperationResult> StartCountDownAsync(string templateType, int duration, CountDownStopMode action, long endTimestamp)
		{
			if (/* აქ შეგიძლია დაამატო GameMode შემოწმება, თუ injected GameState */ true)
			{
				try
				{
					var message = new { type = action.ToString(), endTime = endTimestamp };
					await _casparCGWsService.SendDataToTemplateAsync(templateType, message);
					_logger.LogInformationWithCaller($"Started countdown for '{templateType}' ({action}) with {duration}s.");
					return new OperationResult(true);
				}
				catch (Exception ex)
				{
					_logger.LogError($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Error starting countdown for '{templateType} {ex}'.");
					return new OperationResult(false, ex.Message);
				}
			}
			return new OperationResult(false, "CountDown disabled in current mode.");
		}

		public async Task<OperationResult> PlayClipAsync(int channel, int layer, string clipName)
		{
			try
			{
				var templateName = _cgSettings?.QuestionVideo.TemplateUrl + clipName;
				if (!string.IsNullOrEmpty(templateName))
				{
					templateName = templateName.Replace("\\", "/").Replace(".mp4", "");
					await _caspar.PlayClip(channel, layer, templateName);
					_logger.LogInformationWithCaller($"Played clip '{clipName}' on channel {channel}, layer {layer}.");
					return new OperationResult(true);
				}
				return new OperationResult(false, "Invalid clip name.");
			}
			catch (Exception ex)
			{
				_logger.LogError($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Error playing clip '{clipName}' {ex}.");
				return new OperationResult(false, ex.Message);
			}
		}

		public async Task<OperationResult> ClearPlayClipAsync(CGTemplateEnums templateType)
		{
			try
			{
				var _cgTemTemplateSetting = _cgSettingsMap.GetValueOrDefault(templateType);
				
				await _caspar.ClearChannelLayer(_cgTemTemplateSetting.Channel, _cgTemTemplateSetting.Layer);
				_logger.LogInformationWithCaller($"Cleared play clip for {templateType}.");
				return new OperationResult(true);
			}
			catch (Exception ex)
			{
				_logger.LogError($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Error clearing play clip for {templateType} {ex}.");
				return new OperationResult(false, ex.Message);
			}
		}


		public async Task<OperationResult> CGSWShowFinalResultsAsync()
		{
			var players = _gameService.ConnectedPlayers.Values.Where(p => p.Score > 0 && p.ClientType == ClientTypes.Contestant.ToString())
				.Select(p => new { id = p.ConnectionId, name = p.Name, score = p.Score })
				.OrderByDescending(s => s.score)
				.ToList();

			var message = new
			{
				type = "show_final_results",
				players = players
			};

			_ = await _casparCGWsService.SendDataToTemplateAsync("LeaderBoard", message);
			return new OperationResult(true);
			///_ = await _casparManager.SendDataToTemplateAsync("LeaderBoard", message);
		}

		public async Task<OperationResult> CGSWToggleLeaderBoardAsync(bool isLeaderBoardActive, bool isVisible)
		{
			if (!isVisible)
			{
				var message = new { type = "clear" };
				_ = await _casparCGWsService.SendDataToTemplateAsync("LeaderBoard", message);
			}
			else
			{
				await CGSWUpdateLeaderBoardAsync(isLeaderBoardActive);
			}
			return new OperationResult(true);
		}

		public async Task CGSWUpdateLeaderBoardAsync(bool isLeaderBoardActive, bool isFinal = false)
		{
			_logger.LogInformationWithCaller($"UpdateLeaderBoard isFinal '{isFinal}'");

			if (!isLeaderBoardActive && !isFinal) return;

			var players = _gameService.ConnectedPlayers.Values
				.Where(p => p.ClientType == "Contestant")
				.OrderByDescending(p => p.Score)
				.Select(p => new { id = p.ConnectionId, name = p.NickName, score = p.Score })
				.ToList();

			var message = new
			{
				type = isFinal ? "show_final_results" : "update_LeaderBoard",
				players = players
			};

			_ = await _casparCGWsService.SendDataToTemplateAsync("LeaderBoard", message);
		}


		public async Task CGSWStoreFinalResultsAsync()
		{
			await Task.Run(() =>
			{
				var _finalScores = _gameService.ConnectedPlayers.Values
					.Select(p => new PlayerScore
					{
						PlayerId = p.ConnectionId,
						PlayerName = p.Name,
						Score = p.Score,
						Timestamp = DateTime.Now
					})
					.ToList();
			});

			_logger.LogInformationWithCaller($"Final results stored for session.");
		}


		public async Task CGWSYTVoteAsync(string templateType, VoteResultsMessage message)
		{
			_logger.LogInformationWithCaller($"YT graphic  sec");

			;

			// Use the new method that targets a specific template
			_ = await _casparCGWsService.SendDataToTemplateAsync(templateType, message);
		}

		public async Task CGWSUpdateInQuestionShowCorrectTemplateDataAsync(string templateType, object message)

		{
			_logger.LogInformationWithCaller($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Operator requested to update template fro Correct Answer '{templateType}' {message}");

			_ = await _casparCGWsService.SendDataToTemplateAsync(templateType, message);
		}

		public async Task CGWSClearChannelLayerAsync(CGTemplateEnums templateType)
		{


			var data = new
			{
				type = "clear_content",
				Question = "",
				QuestionImage = "",
				Answers = ""
			};

			_ = await _casparCGWsService.SendDataToTemplateAsync(templateType.ToString(), data);



		}



	}
}
