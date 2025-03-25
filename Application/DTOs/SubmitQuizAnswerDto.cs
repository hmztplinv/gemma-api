public class SubmitQuizAnswerDto
    {
        public int QuizId { get; set; }
        public List<QuizAnswerItem> Answers { get; set; } = new List<QuizAnswerItem>();
    }