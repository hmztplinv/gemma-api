using System;
using System.Collections.Generic;

namespace LanguageLearningApp.API.Application.DTOs
{
    public class ConversationDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastMessageAt { get; set; }
        public List<MessageDto> Messages { get; set; } = new List<MessageDto>();
    }
}
