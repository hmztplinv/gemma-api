using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageLearningApp.API.Application.DTOs;

namespace LanguageLearningApp.API.Domain.Interfaces
{
    public interface IGoalService
    {
        Task<IEnumerable<GoalDto>> GetUserGoalsAsync(int userId);
        Task<GoalDto> GetUserGoalByIdAsync(int userId, int goalId);
        Task<GoalDto> CreateUserGoalAsync(int userId, GoalDto goalDto);
        Task<GoalDto> UpdateUserGoalAsync(int userId, int goalId, GoalDto goalDto);
        Task<bool> DeleteUserGoalAsync(int userId, int goalId);
    }
}