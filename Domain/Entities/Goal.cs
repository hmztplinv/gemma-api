namespace LanguageLearningApp.API.Domain.Entities
{
    public class Goal
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string GoalType { get; set; } // Daily, Weekly
        public string TargetType { get; set; } // Words, Quizzes, Conversations
        public int DefaultTargetValue { get; set; }
        
        // Navigation properties
        public virtual ICollection<UserGoal> UserGoals { get; set; }
    }
}