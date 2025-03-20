using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageLearningApp.API.Domain.Entities;

namespace LanguageLearningApp.API.Domain.Interfaces
{
    public interface IMessageRepository : IRepository<Message>
    {
        Task<IEnumerable<Message>> GetMessagesByConversationIdAsync(int conversationId, int take = 10);
    }
}