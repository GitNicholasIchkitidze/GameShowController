using GameController.Shared.Models;

namespace GameController.Server.Services
{
    public interface IQuestionService
    {
        Task<List<QuestionModel>> LoadQuestionsAsync();


    }
}
