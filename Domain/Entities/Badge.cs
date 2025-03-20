namespace LanguageLearningApp.API.Domain.Entities
{
    public class Badge
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string RequirementType { get; set; } // Words, Quizzes, Streak, etc.
        public int RequirementValue { get; set; } // Number needed to achieve badge
        
        // Navigation properties
        public virtual ICollection<UserBadge> UserBadges { get; set; }
    }
}