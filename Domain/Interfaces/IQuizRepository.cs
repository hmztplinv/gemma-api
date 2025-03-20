using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageLearningApp.API.Domain.Entities;

namespace LanguageLearningApp.API.Domain.Interfaces
{
    public interface IQuizRepository : IRepository<Quiz>
    {
        Task<IEnumerable<Quiz>> GetQuizzesByLevelAsync(string level);
    }
}