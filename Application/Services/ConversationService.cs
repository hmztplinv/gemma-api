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
    public class ConversationService : IConversationService
    {
        private readonly IConversationRepository _conversationRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUserVocabularyRepository _userVocabularyRepository;
        private readonly IUserProgressRepository _userProgressRepository;
        private readonly ILlmService _llmService;
        private readonly ILogger<ConversationService> _logger;

        public ConversationService(
            IConversationRepository conversationRepository,
            IMessageRepository messageRepository,
            IUserRepository userRepository,
            IUserVocabularyRepository userVocabularyRepository,
            IUserProgressRepository userProgressRepository,
            ILlmService llmService,
            ILogger<ConversationService> logger)
        {
            _conversationRepository = conversationRepository;
            _messageRepository = messageRepository;
            _userRepository = userRepository;
            _userVocabularyRepository = userVocabularyRepository;
            _userProgressRepository = userProgressRepository;
            _llmService = llmService;
            _logger = logger;
        }

        public async Task<IEnumerable<ConversationDto>> GetUserConversationsAsync(int userId)
        {
            var conversations = await _conversationRepository.GetUserConversationsAsync(userId);

            return conversations.Select(c => new ConversationDto
            {
                Id = c.Id,
                Title = c.Title,
                CreatedAt = c.CreatedAt,
                LastMessageAt = c.LastMessageAt
            });
        }

        public async Task<ConversationDto> GetConversationAsync(int conversationId, int userId)
        {
            var conversation = await _conversationRepository.GetConversationWithMessagesAsync(conversationId);

            if (conversation == null || conversation.UserId != userId)
            {
                throw new UnauthorizedAccessException("Conversation not found or access denied");
            }

            var messageDtos = conversation.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new MessageDto
                {
                    Id = m.Id,
                    ConversationId = m.ConversationId,
                    IsFromUser = m.IsFromUser,
                    Content = m.Content,
                    CreatedAt = m.CreatedAt,
                    ErrorAnalysis = !string.IsNullOrEmpty(m.ErrorAnalysis) && m.IsFromUser
                        ? JsonSerializer.Deserialize<ErrorAnalysisDto>(m.ErrorAnalysis)
                        : null
                })
                .ToList();

            return new ConversationDto
            {
                Id = conversation.Id,
                Title = conversation.Title,
                CreatedAt = conversation.CreatedAt,
                LastMessageAt = conversation.LastMessageAt,
                Messages = messageDtos
            };
        }

        // ConversationService.cs içine eklenecek
        private async Task UpdateCorrectUsageAsync(int userId, string message, ErrorAnalysisDto analysis)
        {
            // Analiz içinde hata olmayan kelimeleri doğru kullanılmış say
            var userVocabulary = await _userVocabularyRepository.GetUserVocabularyAsync(userId);

            foreach (var vocabularyItem in userVocabulary)
            {
                if (message.ToLower().Contains(vocabularyItem.Word.ToLower()))
                {
                    // Kelime mesajda var, hatalı mı değil mi kontrol et
                    bool hasError = false;

                    if (analysis?.Errors != null)
                    {
                        foreach (var error in analysis.Errors)
                        {
                            if (error.ErrorText.ToLower().Contains(vocabularyItem.Word.ToLower()))
                            {
                                hasError = true;
                                break;
                            }
                        }
                    }

                    if (!hasError)
                    {
                        vocabularyItem.TimesCorrectlyUsed++;

                        // 5 kez doğru kullanıldıysa öğrenildi say
                        if (vocabularyItem.TimesCorrectlyUsed >= 3)
                        {
                            vocabularyItem.IsMastered = true;
                        }

                        _userVocabularyRepository.Update(vocabularyItem);
                    }
                }
            }

            await _userVocabularyRepository.SaveChangesAsync();
        }
        public async Task<ConversationDto> CreateConversationAsync(int userId, CreateConversationDto dto)
        {
            // Create new conversation
            var conversation = new Conversation
            {
                UserId = userId,
                Title = dto.Title,
                CreatedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow
            };

            await _conversationRepository.AddAsync(conversation);
            await _conversationRepository.SaveChangesAsync();

            // Add initial user message
            var userMessage = new Message
            {
                ConversationId = conversation.Id,
                IsFromUser = true,
                Content = dto.InitialMessage,
                CreatedAt = DateTime.UtcNow
            };

            await _messageRepository.AddAsync(userMessage);
            await _messageRepository.SaveChangesAsync();

            // Analyze errors in user message
            var errorAnalysis = await _llmService.AnalyzeErrorsAsync(dto.InitialMessage);
            userMessage.ErrorAnalysis = errorAnalysis;
            _messageRepository.Update(userMessage);
            await _messageRepository.SaveChangesAsync();

            // Process vocabulary from user message
            await ProcessVocabularyAsync(userId, dto.InitialMessage);

            // Get AI response
            var messages = new List<Message> { userMessage };
            var aiResponseContent = await _llmService.GetResponseAsync(dto.InitialMessage, messages);

            // Add AI response
            var aiMessage = new Message
            {
                ConversationId = conversation.Id,
                IsFromUser = false,
                Content = aiResponseContent,
                CreatedAt = DateTime.UtcNow
            };

            await _messageRepository.AddAsync(aiMessage);

            // Update conversation last message timestamp
            conversation.LastMessageAt = DateTime.UtcNow;
            _conversationRepository.Update(conversation);

            // Update user progress
            await UpdateUserProgressAsync(userId, true);

            await _messageRepository.SaveChangesAsync();
            await _conversationRepository.SaveChangesAsync();

            // Return conversation DTO
            return await GetConversationAsync(conversation.Id, userId);
        }

        public async Task<MessageDto> SendMessageAsync(int conversationId, int userId, SendMessageDto dto)
        {
            var conversation = await _conversationRepository.GetConversationWithMessagesAsync(conversationId);

            if (conversation == null || conversation.UserId != userId)
            {
                throw new UnauthorizedAccessException("Conversation not found or access denied");
            }

            // Add user message
            var userMessage = new Message
            {
                ConversationId = conversationId,
                IsFromUser = true,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow
            };

            await _messageRepository.AddAsync(userMessage);
            await _messageRepository.SaveChangesAsync();

            // Analyze errors in user message
            var errorAnalysis = await _llmService.AnalyzeErrorsAsync(dto.Content);
            userMessage.ErrorAnalysis = errorAnalysis;
            _messageRepository.Update(userMessage);
            await _messageRepository.SaveChangesAsync();

            // Doğru kullanımı güncelle
            try
            {
                // Farklı bir değişken adı kullanın
                var errorAnalysisObject = JsonSerializer.Deserialize<ErrorAnalysisDto>(errorAnalysis);
                await UpdateCorrectUsageAsync(userId, dto.Content, errorAnalysisObject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating correct usage");
            }

            // Process vocabulary from user message
            await ProcessVocabularyAsync(userId, dto.Content);

            // Get recent conversation history (last 10 messages)
            var recentMessages = (await _messageRepository.GetMessagesByConversationIdAsync(conversationId, 10))
                .OrderBy(m => m.CreatedAt)
                .ToList();

            // Get AI response
            var aiResponseContent = await _llmService.GetResponseAsync(dto.Content, recentMessages);

            // Add AI response
            var aiMessage = new Message
            {
                ConversationId = conversationId,
                IsFromUser = false,
                Content = aiResponseContent,
                CreatedAt = DateTime.UtcNow
            };

            await _messageRepository.AddAsync(aiMessage);

            // Update conversation last message timestamp
            conversation.LastMessageAt = DateTime.UtcNow;
            _conversationRepository.Update(conversation);

            // Update user progress
            await UpdateUserProgressAsync(userId);

            await _messageRepository.SaveChangesAsync();
            await _conversationRepository.SaveChangesAsync();

            // Parse error analysis for DTO
            ErrorAnalysisDto errorAnalysisDto = null;
            if (!string.IsNullOrEmpty(errorAnalysis))
            {
                try
                {
                    errorAnalysisDto = JsonSerializer.Deserialize<ErrorAnalysisDto>(errorAnalysis);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing error analysis JSON");
                }
            }

            // Return message DTO
            return new MessageDto
            {
                Id = userMessage.Id,
                ConversationId = userMessage.ConversationId,
                IsFromUser = userMessage.IsFromUser,
                Content = userMessage.Content,
                CreatedAt = userMessage.CreatedAt,
                ErrorAnalysis = errorAnalysisDto
            };
        }

        private async Task ProcessVocabularyAsync(int userId, string message)
        {
            try
            {
                _logger.LogInformation($"Starting vocabulary processing for message: '{message}'");

                // Extract vocabulary from message
                var words = await _llmService.ExtractVocabularyAsync(message);
                _logger.LogInformation($"Extracted words: {JsonSerializer.Serialize(words)}");

                if (words == null || words.Count == 0)
                {
                    _logger.LogWarning("No words extracted from message");
                    return;
                }

                foreach (var word in words)
                {
                    // Skip short or empty words
                    if (string.IsNullOrWhiteSpace(word) || word.Length < 3)
                    {
                        _logger.LogInformation($"Skipping short word: '{word}'");
                        continue;
                    }

                    // Check if the word is already in the user's vocabulary
                    var existingWord = await _userVocabularyRepository.GetUserVocabularyByWordAsync(userId, word);
                    _logger.LogInformation($"Word '{word}' exists in DB: {existingWord != null}");

                    if (existingWord != null)
                    {
                        // Update existing word
                        existingWord.TimesEncountered++;
                        existingWord.LastEncounteredAt = DateTime.UtcNow;
                        _userVocabularyRepository.Update(existingWord);
                        _logger.LogInformation($"Updated word '{word}', encounters: {existingWord.TimesEncountered}");
                    }
                    else
                    {
                        // Determine word level
                        var level = await _llmService.GetVocabularyLevelAsync(word);
                        _logger.LogInformation($"Determined level for '{word}': {level}");

                        // Add new word to vocabulary
                        var newWord = new UserVocabulary
                        {
                            UserId = userId,
                            Word = word,
                            Translation = "",
                            Level = level,
                            TimesEncountered = 1,
                            TimesCorrectlyUsed = 0,
                            FirstEncounteredAt = DateTime.UtcNow,
                            LastEncounteredAt = DateTime.UtcNow,
                            IsMastered = false
                        };

                        await _userVocabularyRepository.AddAsync(newWord);
                        _logger.LogInformation($"Added new word to vocabulary: '{word}'");
                    }
                }

                var saveResult = await _userVocabularyRepository.SaveChangesAsync();
                _logger.LogInformation($"SaveChanges result: {saveResult}, indicates {saveResult} affected rows");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing vocabulary: {ErrorMessage}", ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerErrorMessage}", ex.InnerException.Message);
                }
            }
        }

        private async Task UpdateUserProgressAsync(int userId, bool isNewConversation = false)
        {
            try
            {
                var user = await _userRepository.GetUserWithDetailsAsync(userId);
                if (user == null) return;

                var progress = await _userProgressRepository.GetUserProgressAsync(userId);
                if (progress == null)
                {
                    progress = new UserProgress
                    {
                        UserId = userId,
                        TotalWordsLearned = 0,
                        TotalQuizzesTaken = 0,
                        AverageQuizScore = 0,
                        DailyGoalCompletedCount = 0,
                        WeeklyGoalCompletedCount = 0,
                        TotalConversations = 0,
                        TotalMessages = 0,
                        LastUpdatedAt = DateTime.UtcNow
                    };

                    await _userProgressRepository.AddAsync(progress);
                    await _userProgressRepository.SaveChangesAsync();
                }

                // Update message count
                progress.TotalMessages++;

                // Update conversation count if it's a new conversation
                if (isNewConversation)
                {
                    progress.TotalConversations++;
                }

                // Get vocabulary count
                var vocabularyCount = (await _userVocabularyRepository.GetUserVocabularyAsync(userId)).Count();
                progress.TotalWordsLearned = vocabularyCount;

                // Update timestamp
                progress.LastUpdatedAt = DateTime.UtcNow;

                // Update user streak if it's a new day
                var lastActiveDate = user.LastActive.Date;
                var today = DateTime.UtcNow.Date;

                if (lastActiveDate < today)
                {
                    // If last active was yesterday, increase streak
                    if (lastActiveDate.AddDays(1) == today)
                    {
                        user.CurrentStreak++;

                        // Update longest streak if current is higher
                        if (user.CurrentStreak > user.LongestStreak)
                        {
                            user.LongestStreak = user.CurrentStreak;
                        }
                    }
                    // If more than one day gap, reset streak
                    else
                    {
                        user.CurrentStreak = 1;
                    }

                    // Update last active
                    user.LastActive = DateTime.UtcNow;
                }

                _userProgressRepository.Update(progress);
                _userRepository.Update(user);

                await _userProgressRepository.SaveChangesAsync();
                await _userRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user progress");
            }
        }
    }
}