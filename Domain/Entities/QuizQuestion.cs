using System.Collections.Generic;

namespace LanguageLearningApp.API.Domain.Entities
{
    public class QuizQuestion
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public string Question { get; set; }
        public string CorrectAnswer { get; set; }
        public string Options { get; set; } // JSON array of possible answers
        public string Explanation { get; set; }
        
        // Navigation properties
        public virtual Quiz Quiz { get; set; }
    }
}