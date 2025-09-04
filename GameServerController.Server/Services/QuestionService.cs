using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using GameController.Shared.Models;
using Microsoft.Extensions.Hosting;

namespace GameController.Server.Services
{
	public class QuestionService : IQuestionService
	{
		private readonly IHostEnvironment _env;

		public QuestionService(IHostEnvironment env)
		{
			_env = env;
		}

		public async Task<List<QuestionModel>> LoadQuestionsAsync()
		{
			var filePath = Path.Combine(_env.ContentRootPath, "QuestionBank", "QuestionBank.json");
			if (!File.Exists(filePath))
			{
				return new List<QuestionModel>();
			}

			var jsonString = await File.ReadAllTextAsync(filePath);
			return JsonSerializer.Deserialize<List<QuestionModel>>(jsonString) ?? new List<QuestionModel>();
		}
	}
}