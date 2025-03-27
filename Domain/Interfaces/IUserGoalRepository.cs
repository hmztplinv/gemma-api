using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageLearningApp.API.Domain.Entities;

namespace LanguageLearningApp.API.Domain.Interfaces
{
    public interface IUserGoalRepository : IRepository<UserGoal>
    {
        Task<IEnumerable<UserGoal>> GetUserGoalsAsync(int userId);
        Task<UserGoal> GetUserGoalByIdAsync(int userId, int goalId);
    }
}