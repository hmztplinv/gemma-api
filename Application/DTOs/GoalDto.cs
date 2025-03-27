public class GoalDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string TargetType { get; set; } // words, conversations, minutes, quizzes
        public int TargetValue { get; set; }
        public int CurrentProgress { get; set; }
        public string Frequency { get; set; } // daily, weekly
        public bool IsCompleted { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Category { get; set; }
        public int Progress { get; set; } // Percentage progress
    }