using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageLearningApp.API.Domain.Entities;

namespace LanguageLearningApp.API.Domain.Interfaces
{
    public interface IConversationRepository : IRepository<Conversation>
    {
        Task<Conversation> GetConversationWithMessagesAsync(int conversationId);
        Task<IEnumerable<Conversation>> GetUserConversationsAsync(int userId);
        Task<IEnumerable<Message>> GetConversationMessagesAsync(int conversationId, int take = 10);
    }
}