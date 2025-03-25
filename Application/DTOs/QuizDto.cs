public class QuizDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Level { get; set; }
        public string QuizType { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<QuizQuestionDto> Questions { get; set; } = new List<QuizQuestionDto>();
    }