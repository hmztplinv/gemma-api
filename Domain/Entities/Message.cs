using System;

namespace LanguageLearningApp.API.Domain.Entities
{
    public class Message
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public bool IsFromUser { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ErrorAnalysis { get; set; } = "{}";
        
        // Navigation properties
        public virtual Conversation Conversation { get; set; }
    }
}