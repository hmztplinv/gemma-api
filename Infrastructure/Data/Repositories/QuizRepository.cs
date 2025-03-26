using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageLearningApp.API.Domain.Entities;
using LanguageLearningApp.API.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearningApp.API.Infrastructure.Data.Repositories
{
    public class QuizRepository : RepositoryBase<Quiz>, IQuizRepository
    {
        public QuizRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Quiz>> GetQuizzesByLevelAsync(string level)
        {
            return await _context.Quizzes
                .Where(q => q.Level == level)
                .Include(q => q.Questions)
                .ToListAsync();
        }

        public async Task<Quiz> GetByIdAsync(int id)
        {

            return await _context.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == id);
        }
    }
}