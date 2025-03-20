using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageLearningApp.API.Domain.Entities;
using LanguageLearningApp.API.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearningApp.API.Infrastructure.Data.Repositories
{
    public class ConversationRepository : RepositoryBase<Conversation>, IConversationRepository
    {
        public ConversationRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Conversation> GetConversationWithMessagesAsync(int conversationId)
        {
            return await _context.Conversations
                .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt))
                .FirstOrDefaultAsync(c => c.Id == conversationId);
        }

        public async Task<IEnumerable<Conversation>> GetUserConversationsAsync(int userId)
        {
            return await _context.Conversations
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Message>> GetConversationMessagesAsync(int conversationId, int take = 10)
        {
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(take)
                .ToListAsync();
        }
    }
}