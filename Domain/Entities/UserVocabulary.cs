using System;

namespace LanguageLearningApp.API.Domain.Entities
{
    public class UserVocabulary
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Word { get; set; }
        public string Translation { get; set; }
        public string Level { get; set; } // A1, A2, B1, B2, C1, C2
        public int TimesEncountered { get; set; }
        public int TimesCorrectlyUsed { get; set; }
        public DateTime FirstEncounteredAt { get; set; }
        public DateTime LastEncounteredAt { get; set; }
        public bool IsMastered { get; set; }
        
        // Navigation properties
        public virtual User User { get; set; }
    }
}