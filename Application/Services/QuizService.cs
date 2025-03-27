using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LanguageLearningApp.API.Application.DTOs;
using LanguageLearningApp.API.Application.Interfaces;
using LanguageLearningApp.API.Domain.Entities;
using LanguageLearningApp.API.Domain.Interfaces;
using LanguageLearningApp.API.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace LanguageLearningApp.API.Application.Services
{
    public class QuizService : IQuizService
    {
        private readonly IQuizRepository _quizRepository;
        private readonly IQuizResultRepository _quizResultRepository;
        private readonly IUserVocabularyRepository _userVocabularyRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILlmService _llmService;
        private readonly ILogger<QuizService> _logger;

        private readonly AppDbContext _context;

        public QuizService(
            IQuizRepository quizRepository,
            IQuizResultRepository quizResultRepository,
            IUserVocabularyRepository userVocabularyRepository,
            IUserRepository userRepository,
            ILlmService llmService,
            ILogger<QuizService> logger,
            AppDbContext context)
        {
            _quizRepository = quizRepository;
            _quizResultRepository = quizResultRepository;
            _userVocabularyRepository = userVocabularyRepository;
            _userRepository = userRepository;
            _llmService = llmService;
            _logger = logger;
            _context = context;
        }

        public async Task<IEnumerable<QuizDto>> GetQuizzesByLevelAsync(string level)
        {
            var quizzes = await _quizRepository.GetQuizzesByLevelAsync(level);
            var result = new List<QuizDto>();

            foreach (var quiz in quizzes)
            {
                result.Add(MapQuizToDto(quiz));
            }

            return result;
        }

        public async Task<QuizResultDto> GetQuizResultByIdAsync(int resultId, int userId)
{
    try
    {
        _logger.LogInformation($"Fetching quiz result - ResultId: {resultId}, UserId: {userId}");
        
        var quizResult = await _quizResultRepository.GetByIdAsync(resultId);
        
        if (quizResult == null || quizResult.UserId != userId)
        {
            _logger.LogWarning($"Quiz result not found or access denied - ResultId: {resultId}, UserId: {userId}");
            throw new KeyNotFoundException($"Quiz result with ID {resultId} not found");
        }

        var quiz = await _quizRepository.GetByIdAsync(quizResult.QuizId);
        if (quiz == null)
        {
            _logger.LogWarning($"Associated quiz not found - QuizId: {quizResult.QuizId}");
            throw new KeyNotFoundException($"Associated quiz not found");
        }

        // Create the DTO with basic information
        var resultDto = new QuizResultDto
        {
            Id = quizResult.Id,
            QuizId = quizResult.QuizId,
            QuizTitle = quiz.Title,
            QuizLevel = quiz.Level,
            Score = quizResult.Score,
            TotalQuestions = quizResult.TotalQuestions,
            CorrectAnswers = quizResult.CorrectAnswers,
            CompletedAt = quizResult.CompletedAt
        };

        // Create a dictionary of questions for quick lookup
        var questions = quiz.Questions.ToDictionary(q => q.Id, q => q);

        // Parse the answer details JSON if available
        if (!string.IsNullOrEmpty(quizResult.AnswerDetails))
        {
            try
            {
                // Deserialize to a list of objects that match the structure
                var answerDetails = JsonSerializer.Deserialize<List<JsonElement>>(quizResult.AnswerDetails);
                
                if (answerDetails != null)
                {
                    foreach (var detail in answerDetails)
                    {
                        if (detail.TryGetProperty("questionId", out var questionIdElement) &&
                            detail.TryGetProperty("userAnswer", out var userAnswerElement) &&
                            detail.TryGetProperty("correctAnswer", out var correctAnswerElement) &&
                            detail.TryGetProperty("isCorrect", out var isCorrectElement))
                        {
                            int questionId = questionIdElement.GetInt32();
                            
                            if (questions.TryGetValue(questionId, out var question))
                            {
                                resultDto.Answers.Add(new QuizAnswerDetailDto
                                {
                                    QuestionId = questionId,
                                    Question = question.Question,
                                    UserAnswer = userAnswerElement.GetString() ?? string.Empty,
                                    CorrectAnswer = correctAnswerElement.GetString() ?? string.Empty,
                                    IsCorrect = isCorrectElement.GetBoolean()
                                });
                            }
                        }
                    }
                }
                
                _logger.LogInformation($"Successfully processed {resultDto.Answers.Count} answer details");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing or enhancing answer details");
            }
        }

        return resultDto;
    }
    catch (KeyNotFoundException)
    {
        throw;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Error fetching quiz result - ResultId: {resultId}, UserId: {userId}");
        throw;
    }
}

        public async Task<QuizDto> GetQuizByIdAsync(int quizId)
        {
            var quiz = await _quizRepository.GetByIdAsync(quizId);
            if (quiz == null)
            {
                throw new KeyNotFoundException($"Quiz with ID {quizId} not found");
            }

            return MapQuizToDto(quiz);
        }

        // create GetQuizResultByIdAsync method here

        

        public async Task<QuizDto> GenerateVocabularyQuizAsync(int userId, string level, int questionCount = 5)
        {
            try
            {
                _logger.LogInformation($"Starting to generate vocabulary quiz for user {userId} at level {level}");
                
                // Check if user exists
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    throw new KeyNotFoundException($"User with ID {userId} not found");
                }

                // Get user vocabulary at specified level or lower
                var vocabulary = (await _userVocabularyRepository.GetUserVocabularyAsync(userId))
                    .Where(v => GetLevelValue(v.Level) <= GetLevelValue(level))
                    .ToList();

                if (vocabulary.Count < 5)
                {
                    throw new InvalidOperationException("Not enough vocabulary words to generate a quiz. Learn more words first!");
                }

                // Create a new quiz
                var quiz = new Quiz
                {
                    Title = $"{level} Vocabulary Quiz",
                    Level = level,
                    QuizType = "Vocabulary",
                    CreatedAt = DateTime.UtcNow,
                    Questions = new List<QuizQuestion>()
                };

                // Shuffle vocabulary and take random words for the quiz
                var random = new Random();
                var selectedWords = vocabulary
                    .OrderBy(x => random.Next())
                    .Take(Math.Min(questionCount, vocabulary.Count))
                    .ToList();

                _logger.LogInformation($"Selected {selectedWords.Count} words for quiz generation");

                // Extract just the words from the vocabulary items
                var wordStrings = selectedWords.Select(v => v.Word).ToList();

                // Generate questions for all selected words at once using the new method
                var questionsData = await _llmService.GenerateVocabularyQuizAsync(wordStrings, level, questionCount);
                
                _logger.LogInformation($"Generated {questionsData.Count} question data items");

                // Process each question data and add to quiz
                for (int i = 0; i < questionsData.Count; i++)
                {
                    var questionData = questionsData[i];
                    var word = i < selectedWords.Count ? selectedWords[i].Word : "unknown";
                    
                    try
                    {
                        var quizQuestion = new QuizQuestion
                        {
                            Question = questionData.Question ?? $"What is the meaning of '{word}'?",
                            CorrectAnswer = questionData.CorrectAnswer ?? "Unknown",
                            Options = JsonSerializer.Serialize(questionData.Options ?? new string[] { "Option 1", "Option 2", "Option 3", "Option 4" }),
                            Explanation = questionData.Explanation ?? $"This question tests your knowledge of the word '{word}'."
                        };

                        quiz.Questions.Add(quizQuestion);
                        _logger.LogInformation($"Added question about '{word}' to quiz");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error adding question for word '{word}'");
                    }
                }

                // Save the quiz to the database
                await _quizRepository.AddAsync(quiz);
                await _quizRepository.SaveChangesAsync();
                _logger.LogInformation($"Quiz saved with ID {quiz.Id}");

                return MapQuizToDto(quiz);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating vocabulary quiz");
                throw;
            }
        }

        public async Task<QuizResultDto> SubmitQuizAnswersAsync(int userId, SubmitQuizAnswerDto answers)
        {
            try
            {
                _logger.LogInformation($"Quiz submit başlatıldı - UserId: {userId}, QuizId: {answers.QuizId}, Answers count: {answers.Answers?.Count ?? 0}");
                
                // Veri doğrulama kontrolleri
                if (answers == null || answers.Answers == null || !answers.Answers.Any())
                {
                    throw new ArgumentException("Geçerli cevaplar gönderilmedi");
                }
                
                // Get the quiz
                var quiz = await _quizRepository.GetByIdAsync(answers.QuizId);
                if (quiz == null)
                {
                    throw new KeyNotFoundException($"Quiz with ID {answers.QuizId} not found");
                }
                
                // Quiz soruları var mı kontrol et
                if (quiz.Questions == null || !quiz.Questions.Any())
                {
                    _logger.LogWarning($"Quiz {answers.QuizId} has no questions");
                    throw new InvalidOperationException($"Quiz {answers.QuizId} has no questions");
                }

                // Get questions with correct answers
                var questions = quiz.Questions.ToDictionary(q => q.Id, q => q.CorrectAnswer);

                // Calculate score
                int correctAnswers = 0;
                var answerDetails = new List<object>();

                foreach (var answer in answers.Answers)
                {
                    _logger.LogDebug($"Processing answer for question {answer.QuestionId}: '{answer.Answer}'");
                    
                    if (questions.TryGetValue(answer.QuestionId, out var correctAnswer))
                    {
                        bool isCorrect = string.Equals(correctAnswer, answer.Answer, StringComparison.OrdinalIgnoreCase);
                        if (isCorrect)
                        {
                            correctAnswers++;
                        }

                        answerDetails.Add(new
                        {
                            QuestionId = answer.QuestionId,
                            IsCorrect = isCorrect,
                            UserAnswer = answer.Answer,
                            CorrectAnswer = correctAnswer
                        });
                    }
                    else
                    {
                        _logger.LogWarning($"Question ID {answer.QuestionId} not found in quiz {answers.QuizId}");
                    }
                }

                // Calculate score as percentage
                int totalQuestions = questions.Count;
                int score = totalQuestions > 0 ? (correctAnswers * 100) / totalQuestions : 0;

                // Create quiz result
                var quizResult = new QuizResult
                {
                    UserId = userId,
                    QuizId = quiz.Id,
                    Score = score,
                    TotalQuestions = totalQuestions,
                    CorrectAnswers = correctAnswers,
                    CompletedAt = DateTime.UtcNow,
                    AnswerDetails = JsonSerializer.Serialize(answerDetails)
                };

                // Save quiz result
                await _quizResultRepository.AddAsync(quizResult);
                await _quizResultRepository.SaveChangesAsync();
                
                _logger.LogInformation($"Quiz submit başarılı - UserId: {userId}, QuizId: {answers.QuizId}, Score: {score}");

                // Return result
                return new QuizResultDto
                {
                    Id = quizResult.Id,
                    QuizId = quizResult.QuizId,
                    QuizTitle = quiz.Title,
                    QuizLevel = quiz.Level,
                    Score = quizResult.Score,
                    TotalQuestions = quizResult.TotalQuestions,
                    CorrectAnswers = quizResult.CorrectAnswers,
                    CompletedAt = quizResult.CompletedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Quiz submit hatası - UserId: {userId}, QuizId: {answers?.QuizId}");
                throw;
            }
        }

        public async Task<IEnumerable<QuizResultDto>> GetUserQuizResultsAsync(int userId)
        {
            var quizResults = await _quizResultRepository.GetUserQuizResultsAsync(userId);
            var result = new List<QuizResultDto>();

            foreach (var quizResult in quizResults)
            {
                result.Add(new QuizResultDto
                {
                    Id = quizResult.Id,
                    QuizId = quizResult.QuizId,
                    QuizTitle = quizResult.Quiz?.Title ?? "Unknown Quiz",
                    QuizLevel = quizResult.Quiz?.Level ?? "Unknown",
                    Score = quizResult.Score,
                    TotalQuestions = quizResult.TotalQuestions,
                    CorrectAnswers = quizResult.CorrectAnswers,
                    CompletedAt = quizResult.CompletedAt
                });
            }

            return result;
        }

        public async Task<QuizDto> CreateQuizAsync(QuizDto quizDto)
        {
            var quiz = new Quiz
            {
                Title = quizDto.Title,
                Level = quizDto.Level,
                QuizType = quizDto.QuizType,
                CreatedAt = DateTime.UtcNow,
                Questions = new List<QuizQuestion>()
            };

            foreach (var questionDto in quizDto.Questions)
            {
                var question = new QuizQuestion
                {
                    Question = questionDto.Question,
                    CorrectAnswer = questionDto.CorrectAnswer,
                    Options = JsonSerializer.Serialize(questionDto.Options),
                    Explanation = questionDto.Explanation
                };

                quiz.Questions.Add(question);
            }

            await _quizRepository.AddAsync(quiz);
            await _quizRepository.SaveChangesAsync();

            return MapQuizToDto(quiz);
        }

        // Helper methods
        private QuizDto MapQuizToDto(Quiz quiz)
        {
            var quizDto = new QuizDto
            {
                Id = quiz.Id,
                Title = quiz.Title,
                Level = quiz.Level,
                QuizType = quiz.QuizType,
                CreatedAt = quiz.CreatedAt,
                Questions = new List<QuizQuestionDto>()
            };

            if (quiz.Questions != null)
            {
                foreach (var question in quiz.Questions)
                {
                    var options = new string[0];
                    try
                    {
                        options = JsonSerializer.Deserialize<string[]>(question.Options) ?? new string[0];
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error deserializing options for question {question.Id}");
                    }

                    quizDto.Questions.Add(new QuizQuestionDto
                    {
                        Id = question.Id,
                        Question = question.Question,
                        Options = options,
                        Explanation = question.Explanation,
                        CorrectAnswer = question.CorrectAnswer
                    });
                }
            }

            return quizDto;
        }

        private int GetLevelValue(string level)
        {
            return level switch
            {
                "A1" => 1,
                "A2" => 2,
                "B1" => 3,
                "B2" => 4,
                "C1" => 5,
                "C2" => 6,
                _ => 3 // Default to B1
            };
        }
    }
}