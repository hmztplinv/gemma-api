using System;
using System.Collections.Generic;

namespace LanguageLearningApp.API.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastActive { get; set; }
        public string LanguageLevel { get; set; } // Beginner, Intermediate, Advanced, etc.
        public int CurrentStreak { get; set; } // Number of consecutive days active
        public int LongestStreak { get; set; }
        public int TotalPoints { get; set; }
        
        // Navigation properties
        public virtual ICollection<Conversation> Conversations { get; set; }
        public virtual ICollection<UserVocabulary> Vocabulary { get; set; }
        public virtual ICollection<UserBadge> Badges { get; set; }
        public virtual ICollection<UserGoal> Goals { get; set; }
        public virtual ICollection<QuizResult> QuizResults { get; set; }
        public virtual UserProgress Progress { get; set; }
    }
}