using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageLearningApp.API.Domain.Entities;
using LanguageLearningApp.API.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearningApp.API.Infrastructure.Data.Repositories
{
    public class UserVocabularyRepository : RepositoryBase<UserVocabulary>, IUserVocabularyRepository
    {
        public UserVocabularyRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<UserVocabulary>> GetUserVocabularyAsync(int userId)
        {
            return await _context.UserVocabularies
                .Where(v => v.UserId == userId)
                .OrderByDescending(v => v.LastEncounteredAt)
                .ToListAsync();
        }

        public async Task<UserVocabulary> GetUserVocabularyByWordAsync(int userId, string word)
        {
            return await _context.UserVocabularies
                .FirstOrDefaultAsync(v => v.UserId == userId && v.Word.ToLower() == word.ToLower());
        }
    }
}