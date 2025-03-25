public class QuizQuestionDto
    {
        public int Id { get; set; }
        public string Question { get; set; }
        public string[] Options { get; set; }
        public string Explanation { get; set; }
    }