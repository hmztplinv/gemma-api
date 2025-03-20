using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageLearningApp.API.Application.DTOs;

namespace LanguageLearningApp.API.Application.Interfaces
{
    public interface IConversationService
    {
        Task<IEnumerable<ConversationDto>> GetUserConversationsAsync(int userId);
        Task<ConversationDto> GetConversationAsync(int conversationId, int userId);
        Task<ConversationDto> CreateConversationAsync(int userId, CreateConversationDto dto);
        Task<MessageDto> SendMessageAsync(int conversationId, int userId, SendMessageDto dto);
    }
}