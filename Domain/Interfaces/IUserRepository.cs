using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageLearningApp.API.Domain.Entities;

namespace LanguageLearningApp.API.Domain.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User> GetUserByEmailAsync(string email);
        Task<User> GetUserByUsernameAsync(string username);
        Task<User> GetUserWithDetailsAsync(int userId);
        Task<IEnumerable<UserVocabulary>> GetUserVocabularyAsync(int userId);
        Task<IEnumerable<Badge>> GetUserBadgesAsync(int userId);
        Task<IEnumerable<UserGoal>> GetUserGoalsAsync(int userId);
    }
}