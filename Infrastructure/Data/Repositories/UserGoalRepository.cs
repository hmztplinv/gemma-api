using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageLearningApp.API.Domain.Entities;
using LanguageLearningApp.API.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearningApp.API.Infrastructure.Data.Repositories
{
    public class UserGoalRepository : RepositoryBase<UserGoal>, IUserGoalRepository
    {
        public UserGoalRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<UserGoal>> GetUserGoalsAsync(int userId)
        {
            return await _context.UserGoals
                .Where(g => g.UserId == userId)
                .Include(g => g.Goal)
                .ToListAsync();
        }

        public async Task<UserGoal> GetUserGoalByIdAsync(int userId, int goalId)
        {
            return await _context.UserGoals
                .Where(g => g.UserId == userId && g.Id == goalId)
                .Include(g => g.Goal)
                .FirstOrDefaultAsync();
        }
    }
}