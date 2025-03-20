using System;

namespace LanguageLearningApp.API.Domain.Entities
{
    public class UserProgress
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TotalWordsLearned { get; set; }
        public int TotalQuizzesTaken { get; set; }
        public double AverageQuizScore { get; set; }
        public int DailyGoalCompletedCount { get; set; }
        public int WeeklyGoalCompletedCount { get; set; }
        public int TotalConversations { get; set; }
        public int TotalMessages { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        
        // Navigation properties
        public virtual User User { get; set; }
    }
}