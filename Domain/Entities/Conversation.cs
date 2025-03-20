using System;
using System.Collections.Generic;

namespace LanguageLearningApp.API.Domain.Entities
{
    public class Conversation
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastMessageAt { get; set; }
        
        // Navigation properties
        public virtual User User { get; set; }
        public virtual ICollection<Message> Messages { get; set; }
    }
}