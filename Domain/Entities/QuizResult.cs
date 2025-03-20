using System;

namespace LanguageLearningApp.API.Domain.Entities
{
    public class QuizResult
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int QuizId { get; set; }
        public int Score { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public DateTime CompletedAt { get; set; }
        public string AnswerDetails { get; set; } // JSON array of question IDs and whether they were answered correctly
        
        // Navigation properties
        public virtual User User { get; set; }
        public virtual Quiz Quiz { get; set; }
    }
}