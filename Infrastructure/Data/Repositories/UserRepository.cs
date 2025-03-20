using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageLearningApp.API.Domain.Entities;
using LanguageLearningApp.API.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearningApp.API.Infrastructure.Data.Repositories
{
    public class UserRepository : RepositoryBase<User>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .SingleOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .SingleOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        }

        public async Task<User> GetUserWithDetailsAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.Progress)
                .Include(u => u.Vocabulary)
                .Include(u => u.Badges)
                    .ThenInclude(ub => ub.Badge)
                .Include(u => u.Goals)
                    .ThenInclude(ug => ug.Goal)
                .Include(u => u.QuizResults.OrderByDescending(qr => qr.CompletedAt).Take(10))
                    .ThenInclude(qr => qr.Quiz)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<IEnumerable<UserVocabulary>> GetUserVocabularyAsync(int userId)
        {
            return await _context.UserVocabularies
                .Where(v => v.UserId == userId)
                .OrderByDescending(v => v.LastEncounteredAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Badge>> GetUserBadgesAsync(int userId)
        {
            return await _context.UserBadges
                .Where(ub => ub.UserId == userId)
                .Include(ub => ub.Badge)
                .Select(ub => ub.Badge)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserGoal>> GetUserGoalsAsync(int userId)
        {
            return await _context.UserGoals
                .Where(ug => ug.UserId == userId)
                .Include(ug => ug.Goal)
                .OrderBy(ug => ug.EndDate)
                .ToListAsync();
        }
    }
}