using System.Threading.Tasks;
using LanguageLearningApp.API.Domain.Entities;
using LanguageLearningApp.API.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearningApp.API.Infrastructure.Data.Repositories
{
    public class UserProgressRepository : RepositoryBase<UserProgress>, IUserProgressRepository
    {
        public UserProgressRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<UserProgress> GetUserProgressAsync(int userId)
        {
            return await _context.UserProgresses
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }
    }
}