using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageLearningApp.API.Application.DTOs;

namespace LanguageLearningApp.API.Application.Interfaces
{
    public interface IQuizService
    {
        /// <summary>
        /// Get a list of quizzes by level
        /// </summary>
        Task<IEnumerable<QuizDto>> GetQuizzesByLevelAsync(string level);
        
        /// <summary>
        /// Get a specific quiz by ID
        /// </summary>
        Task<QuizDto> GetQuizByIdAsync(int quizId);

        Task<QuizResultDto> GetQuizResultByIdAsync(int resultId, int userId);
        
        /// <summary>
        /// Generate a vocabulary quiz based on user's vocabulary
        /// </summary>
        Task<QuizDto> GenerateVocabularyQuizAsync(int userId, string level, int questionCount = 5);
        
        /// <summary>
        /// Submit quiz answers and get results
        /// </summary>
        Task<QuizResultDto> SubmitQuizAnswersAsync(int userId, SubmitQuizAnswerDto answers);
        
        /// <summary>
        /// Get quiz results for a user
        /// </summary>
        Task<IEnumerable<QuizResultDto>> GetUserQuizResultsAsync(int userId);
        
        /// <summary>
        /// Create a new quiz (admin functionality)
        /// </summary>
        Task<QuizDto> CreateQuizAsync(QuizDto quiz);
    }
}