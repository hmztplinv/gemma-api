public interface IQuizService
    {
        // Get a list of quizzes by level
        Task<IEnumerable<QuizDto>> GetQuizzesByLevelAsync(string level);
        
        // Get a specific quiz by ID
        Task<QuizDto> GetQuizByIdAsync(int quizId);
        
        // Get a quiz for a user to take (generates questions based on their vocabulary)
        Task<QuizDto> GenerateVocabularyQuizAsync(int userId, string level, int questionCount = 10);
        
        // Submit quiz answers and get results
        Task<QuizResultDto> SubmitQuizAnswersAsync(int userId, SubmitQuizAnswerDto answers);
        
        // Get quiz results for a user
        Task<IEnumerable<QuizResultDto>> GetUserQuizResultsAsync(int userId);
        
        // Create a new quiz (admin functionality)
        Task<QuizDto> CreateQuizAsync(QuizDto quiz);
    }