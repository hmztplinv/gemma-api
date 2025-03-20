using System;

namespace LanguageLearningApp.API.Domain.Entities
{
    public class UserBadge
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BadgeId { get; set; }
        public DateTime AchievedAt { get; set; }
        
        // Navigation properties
        public virtual User User { get; set; }
        public virtual Badge Badge { get; set; }
    }
}