using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageLearningApp.API.Domain.Entities;
using LanguageLearningApp.API.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearningApp.API.Infrastructure.Data.Repositories
{
    public class MessageRepository : RepositoryBase<Message>, IMessageRepository
    {
        public MessageRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Message>> GetMessagesByConversationIdAsync(int conversationId, int take = 10)
        {
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(take)
                .ToListAsync();
        }
    }
}