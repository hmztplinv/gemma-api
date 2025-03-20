using System;

namespace LanguageLearningApp.API.Domain.Entities
{
    public class UserGoal
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int GoalId { get; set; }
        public int CustomTargetValue { get; set; }
        public int CurrentProgress { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        
        // Navigation properties
        public virtual User User { get; set; }
        public virtual Goal Goal { get; set; }
    }
}