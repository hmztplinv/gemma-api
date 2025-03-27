public class QuizAnswerDetailDto
{
    public int QuestionId { get; set; }
    public string Question { get; set; }
    public string UserAnswer { get; set; }
    public string CorrectAnswer { get; set; }
    public bool IsCorrect { get; set; }
}