using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageLearningApp.API.Domain.Entities;
using LanguageLearningApp.API.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearningApp.API.Infrastructure.Data.Repositories
{
    public class UserBadgeRepository : RepositoryBase<UserBadge>, IUserBadgeRepository
    {
        public UserBadgeRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<UserBadge>> GetUserBadgesAsync(int userId)
        {
            return await _context.UserBadges
                .Where(b => b.UserId == userId)
                .Include(b => b.Badge)
                .ToListAsync();
        }

        public async Task<UserBadge> GetUserBadgeByIdAsync(int userId, int badgeId)
        {
            return await _context.UserBadges
                .Where(b => b.UserId == userId && b.BadgeId == badgeId)
                .Include(b => b.Badge)
                .FirstOrDefaultAsync();
        }
    }
}