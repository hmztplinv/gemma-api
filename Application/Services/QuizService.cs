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
            return quizzes.Select(quiz => MapQuizToDto(quiz)).ToList();
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

        public async Task<QuizDto> GenerateVocabularyQuizAsync(int userId, string level, int questionCount = 5)
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

                // Get word list
                var wordList = selectedWords.Select(v => v.Word).ToList();

                // Generate quiz content from LLM
                var quizContent = await _llmService.GenerateVocabularyQuizAsync(wordList, level, questionCount);

                // Parse the quiz content
                var parsedQuestions = ParseQuizContent(quizContent);

                // Add questions to the quiz
                foreach (var parsedQuestion in parsedQuestions)
                {
                    var options = new[] 
                    {
                        parsedQuestion.OptionA,
                        parsedQuestion.OptionB,
                        parsedQuestion.OptionC,
                        parsedQuestion.OptionD
                    };

                    string correctAnswer = parsedQuestion.CorrectOption switch
                    {
                        "a" => parsedQuestion.OptionA,
                        "b" => parsedQuestion.OptionB,
                        "c" => parsedQuestion.OptionC,
                        "d" => parsedQuestion.OptionD,
                        _ => parsedQuestion.OptionA // Default to first option
                    };

                    var quizQuestion = new QuizQuestion
                    {
                        Question = parsedQuestion.QuestionText,
                        CorrectAnswer = correctAnswer,
                        Options = JsonSerializer.Serialize(options),
                        Explanation = $"This tests your knowledge of English vocabulary."
                    };

                    quiz.Questions.Add(quizQuestion);
                }

                // If parsing failed or insufficient questions were created, add fallback questions
                if (quiz.Questions.Count < questionCount)
                {
                    int additionalNeeded = questionCount - quiz.Questions.Count;
                    _logger.LogWarning($"Only {quiz.Questions.Count} questions were created, adding {additionalNeeded} fallback questions");
                    
                    for (int i = 0; i < additionalNeeded && i < selectedWords.Count; i++)
                    {
                        var word = selectedWords[i];
                        if (!quiz.Questions.Any(q => q.Question.Contains(word.Word)))
                        {
                            var fallbackQuestion = CreateFallbackQuestion(word.Word, level);
                            quiz.Questions.Add(fallbackQuestion);
                        }
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
            
            return quizResults.Select(result => new QuizResultDto
            {
                Id = result.Id,
                QuizId = result.QuizId,
                QuizTitle = result.Quiz?.Title ?? "Unknown Quiz",
                QuizLevel = result.Quiz?.Level ?? "Unknown",
                Score = result.Score,
                TotalQuestions = result.TotalQuestions,
                CorrectAnswers = result.CorrectAnswers,
                CompletedAt = result.CompletedAt
            }).ToList();
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
        private List<ParsedQuestion> ParseQuizContent(string quizContent)
        {
            var result = new List<ParsedQuestion>();
            
            // Split by "Question" to get individual question blocks
            var questionBlocks = quizContent.Split(new[] { "Question" }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var block in questionBlocks)
            {
                if (string.IsNullOrWhiteSpace(block))
                    continue;
                
                try
                {
                    // Split into lines
                    var lines = block.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(l => l.Trim())
                                    .Where(l => !string.IsNullOrWhiteSpace(l))
                                    .ToList();
                    
                    if (lines.Count < 5) // We need at least the question and 4 options
                        continue;
                    
                    // Extract question text
                    string questionText = lines[0];
                    if (questionText.Contains(":"))
                        questionText = questionText.Substring(questionText.IndexOf(':') + 1).Trim();
                    
                    // Find options
                    string optionA = FindOption(lines, "a)");
                    string optionB = FindOption(lines, "b)");
                    string optionC = FindOption(lines, "c)");
                    string optionD = FindOption(lines, "d)");
                    
                    // Find correct answer
                    string correctOption = "a"; // Default
                    var correctLine = lines.FirstOrDefault(l => 
                        l.Contains("Correct:") || 
                        l.Contains("correct:") || 
                        l.StartsWith("Correct") || 
                        l.StartsWith("correct"));
                    
                    if (correctLine != null)
                    {
                        var parts = correctLine.Split(':');
                        if (parts.Length > 1)
                        {
                            var answer = parts[1].Trim().ToLower();
                            if (answer.Length > 0)
                                correctOption = answer[0].ToString();
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(questionText) && 
                        !string.IsNullOrEmpty(optionA) && 
                        !string.IsNullOrEmpty(optionB) && 
                        !string.IsNullOrEmpty(optionC) && 
                        !string.IsNullOrEmpty(optionD))
                    {
                        result.Add(new ParsedQuestion
                        {
                            QuestionText = questionText,
                            OptionA = optionA,
                            OptionB = optionB,
                            OptionC = optionC,
                            OptionD = optionD,
                            CorrectOption = correctOption
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Error parsing question: {ex.Message}");
                    // Continue to next question
                }
            }
            
            return result;
        }
        
        private string FindOption(List<string> lines, string prefix)
        {
            var optionLine = lines.FirstOrDefault(l => l.TrimStart().StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            if (optionLine == null)
                return $"Option {prefix[0]}";
                
            int startIndex = optionLine.IndexOf(prefix) + prefix.Length;
            return optionLine.Substring(startIndex).Trim();
        }

        private QuizQuestion CreateFallbackQuestion(string word, string level)
        {
            string[] options;
            string correctAnswer;
            string question;
            
            // Create based on level
            if (level == "A1" || level == "A2")
            {
                question = $"What does '{word}' mean?";
                correctAnswer = $"The correct meaning of '{word}'";
                options = new[] 
                {
                    correctAnswer,
                    "An incorrect meaning",
                    "Another wrong meaning",
                    "A different incorrect meaning"
                };
            }
            else if (level == "B1" || level == "B2")
            {
                question = $"Which sentence uses '{word}' correctly?";
                correctAnswer = $"This sentence uses '{word}' correctly.";
                options = new[] 
                {
                    correctAnswer,
                    $"This sentence uses '{word}' incorrectly.",
                    $"This incorrect sentence has '{word}'.",
                    $"'{word}' is used wrong here."
                };
            }
            else // C1 or C2
            {
                question = $"Which word is closest in meaning to '{word}'?";
                correctAnswer = "A synonym";
                options = new[] 
                {
                    correctAnswer,
                    "An antonym",
                    "An unrelated word",
                    "A different part of speech"
                };
            }
            
            return new QuizQuestion
            {
                Question = question,
                CorrectAnswer = correctAnswer,
                Options = JsonSerializer.Serialize(options),
                Explanation = $"This tests your knowledge of the word '{word}'."
            };
        }

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
        
        // Helper class for parsing LLM responses
        private class ParsedQuestion
        {
            public string QuestionText { get; set; }
            public string OptionA { get; set; }
            public string OptionB { get; set; }
            public string OptionC { get; set; }
            public string OptionD { get; set; }
            public string CorrectOption { get; set; }
        }
    }
}