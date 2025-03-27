using System;

namespace LanguageLearningApp.API.Application.DTOs
{
    public class BadgeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string Category { get; set; }
        public DateTime? EarnedAt { get; set; }
        public bool IsEarned { get; set; }
        public int Progress { get; set; } // Percentage progress toward earning
    }
}