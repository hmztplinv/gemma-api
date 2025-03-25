using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LanguageLearningApp.API.Application.DTOs;
using LanguageLearningApp.API.Application.Interfaces;
using LanguageLearningApp.API.Domain.Entities;
using LanguageLearningApp.API.Domain.Interfaces;
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

        public QuizService(
            IQuizRepository quizRepository,
            IQuizResultRepository quizResultRepository,
            IUserVocabularyRepository userVocabularyRepository,
            IUserRepository userRepository,
            ILlmService llmService,
            ILogger<QuizService> logger)
        {
            _quizRepository = quizRepository;
            _quizResultRepository = quizResultRepository;
            _userVocabularyRepository = userVocabularyRepository;
            _userRepository = userRepository;
            _llmService = llmService;
            _logger = logger;
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

        public async Task<QuizDto> GetQuizByIdAsync(int quizId)
        {
            var quiz = await _quizRepository.GetByIdAsync(quizId);
            if (quiz == null)
            {
                throw new KeyNotFoundException($"Quiz with ID {quizId} not found");
            }

            return MapQuizToDto(quiz);
        }

        public async Task<QuizDto> GenerateVocabularyQuizAsync(int userId, string level, int questionCount = 10)
        {
            try
            {
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

                // Generate questions for each selected word
                foreach (var word in selectedWords)
                {
                    try
                    {
                        var questionJson = await _llmService.GenerateQuizQuestionAsync(word.Word, level);
                        QuizQuestionData questionData = null;

                        try
                        {
                            questionData = JsonSerializer.Deserialize<QuizQuestionData>(questionJson);
                        }
                        catch (JsonException)
                        {
                            _logger.LogWarning($"Failed to parse JSON response for word '{word.Word}'. Response: {questionJson}");

                            // Manuel olarak JSON yanıtı ayıklamaya çalış
                            string question = ExtractQuestionFromText(questionJson);
                            string correctAnswer = ExtractCorrectAnswerFromText(questionJson);
                            string[] options = ExtractOptionsFromText(questionJson);

                            if (!string.IsNullOrEmpty(question) && !string.IsNullOrEmpty(correctAnswer) && options.Length > 0)
                            {
                                questionData = new QuizQuestionData
                                {
                                    Question = question,
                                    CorrectAnswer = correctAnswer,
                                    Options = options,
                                    Explanation = $"This question is about the word '{word.Word}'."  // Varsayılan açıklama
                                };
                            }
                        }

                        if (questionData != null)
                        {
                            var quizQuestion = new QuizQuestion
                            {
                                Question = questionData.Question ?? $"What is the meaning of '{word.Word}'?",
                                CorrectAnswer = questionData.CorrectAnswer ?? word.Word,
                                Options = JsonSerializer.Serialize(questionData.Options ?? new string[] { word.Word, "Option 2", "Option 3", "Option 4" }),
                                Explanation = questionData.Explanation ?? $"This question tests your knowledge of the word '{word.Word}'."
                            };

                            quiz.Questions.Add(quizQuestion);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error generating question for word '{word.Word}'");
                    }
                }

                // Save the quiz to the database
                await _quizRepository.AddAsync(quiz);
                await _quizRepository.SaveChangesAsync();

                return MapQuizToDto(quiz);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating vocabulary quiz");
                throw;
            }
        }
        private string ExtractQuestionFromText(string text)
        {
            // Basit bir regex ile soru cümlesini bulmaya çalış
            var match = System.Text.RegularExpressions.Regex.Match(text, @"question[""']?\s*:\s*[""']([^""']+)[""']", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            // Veya ilk soru işareti ile biten cümleyi al
            match = System.Text.RegularExpressions.Regex.Match(text, @"([^.!?]+\?['""]?)");
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            return null;
        }

        private string ExtractCorrectAnswerFromText(string text)
        {
            var match = System.Text.RegularExpressions.Regex.Match(text, @"correctAnswer[""']?\s*:\s*[""']([^""']+)[""']", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            return null;
        }

        private string[] ExtractOptionsFromText(string text)
        {
            var match = System.Text.RegularExpressions.Regex.Match(text, @"options[""']?\s*:\s*\[(.*?)\]", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            if (match.Success)
            {
                var optionsText = match.Groups[1].Value;
                var options = System.Text.RegularExpressions.Regex.Matches(optionsText, @"[""']([^""']+)[""']")
                    .Cast<System.Text.RegularExpressions.Match>()
                    .Select(m => m.Groups[1].Value.Trim())
                    .ToArray();

                if (options.Length > 0)
                {
                    return options;
                }
            }

            return new string[0];
        }
        public async Task<QuizResultDto> SubmitQuizAnswersAsync(int userId, SubmitQuizAnswerDto answers)
        {
            // Get the quiz
            var quiz = await _quizRepository.GetByIdAsync(answers.QuizId);
            if (quiz == null)
            {
                throw new KeyNotFoundException($"Quiz with ID {answers.QuizId} not found");
            }

            // Get questions with correct answers
            var questions = quiz.Questions.ToDictionary(q => q.Id, q => q.CorrectAnswer);

            // Calculate score
            int correctAnswers = 0;
            var answerDetails = new List<object>();

            foreach (var answer in answers.Answers)
            {
                if (questions.TryGetValue(answer.QuestionId, out var correctAnswer))
                {
                    bool isCorrect = correctAnswer.Equals(answer.Answer, StringComparison.OrdinalIgnoreCase);
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
                    CorrectAnswer = questionDto.Options[0], // Assume first option is correct
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
                        Explanation = question.Explanation
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

    // Helper class for deserializing quiz question data from LLM
    public class QuizQuestionData
    {
        public string Question { get; set; }
        public string[] Options { get; set; }
        public string CorrectAnswer { get; set; }
        public string Explanation { get; set; }
    }
}