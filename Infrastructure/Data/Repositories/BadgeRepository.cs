using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageLearningApp.API.Domain.Entities;
using LanguageLearningApp.API.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearningApp.API.Infrastructure.Data.Repositories
{
    public class BadgeRepository : RepositoryBase<Badge>, IBadgeRepository
    {
        public BadgeRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Badge>> GetAllBadgesAsync()
        {
            return await _context.Badges.ToListAsync();
        }
    }
}