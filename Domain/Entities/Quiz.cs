using System;
using System.Collections.Generic;

namespace LanguageLearningApp.API.Domain.Entities
{
    public class Quiz
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Level { get; set; } // A1, A2, B1, B2, C1, C2
        public string QuizType { get; set; } // Vocabulary, Grammar, etc.
        public DateTime CreatedAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<QuizQuestion> Questions { get; set; }
        public virtual ICollection<QuizResult> Results { get; set; }
    }
}