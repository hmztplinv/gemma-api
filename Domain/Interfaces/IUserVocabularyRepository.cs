using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageLearningApp.API.Domain.Entities;

namespace LanguageLearningApp.API.Domain.Interfaces
{
    public interface IUserVocabularyRepository : IRepository<UserVocabulary>
    {
        Task<IEnumerable<UserVocabulary>> GetUserVocabularyAsync(int userId);
        Task<UserVocabulary> GetUserVocabularyByWordAsync(int userId, string word);
    }
}