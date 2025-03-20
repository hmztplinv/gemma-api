using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageLearningApp.API.Domain.Entities;
using LanguageLearningApp.API.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearningApp.API.Infrastructure.Data.Repositories
{
    public class QuizResultRepository : RepositoryBase<QuizResult>, IQuizResultRepository
    {
        public QuizResultRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<QuizResult>> GetUserQuizResultsAsync(int userId)
        {
            return await _context.QuizResults
                .Where(r => r.UserId == userId)
                .Include(r => r.Quiz)
                .OrderByDescending(r => r.CompletedAt)
                .ToListAsync();
        }
    }
}