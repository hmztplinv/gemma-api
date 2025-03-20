using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageLearningApp.API.Domain.Entities;

namespace LanguageLearningApp.API.Domain.Interfaces
{
    public interface IQuizResultRepository : IRepository<QuizResult>
    {
        Task<IEnumerable<QuizResult>> GetUserQuizResultsAsync(int userId);
    }
}