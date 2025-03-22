using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using LanguageLearningApp.API.Application.DTOs;
using LanguageLearningApp.API.Domain.Entities;
using LanguageLearningApp.API.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LanguageLearningApp.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserVocabularyRepository _userVocabularyRepository;
        private readonly IUserProgressRepository _userProgressRepository;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserRepository userRepository,
            IUserVocabularyRepository userVocabularyRepository,
            IUserProgressRepository userProgressRepository,
            ILogger<UsersController> logger)
        {
            _userRepository = userRepository;
            _userVocabularyRepository = userVocabularyRepository;
            _userProgressRepository = userProgressRepository;
            _logger = logger;
        }

        [HttpGet("profile")]
        public async Task<ActionResult<UserProfileDto>> GetUserProfile()
        {
            try
            {
                var userId = GetUserId();
                var user = await _userRepository.GetUserWithDetailsAsync(userId);

                if (user == null)
                {
                    return NotFound("User not found");
                }

                var profile = new UserProfileDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    LanguageLevel = user.LanguageLevel,
                    NativeLanguage = "Turkish", // Bu bilgiyi eklemek için model güncellemesi gerekebilir
                    LearningLanguage = "English", // Bu bilgiyi eklemek için model güncellemesi gerekebilir
                    MemberSince = user.CreatedAt.ToString("dd.MM.yyyy"),
                    CurrentStreak = user.CurrentStreak,
                    LongestStreak = user.LongestStreak
                };

                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile");
                return StatusCode(500, "Failed to retrieve user profile");
            }
        }

        [HttpGet("progress")]
        public async Task<ActionResult<UserProgressDto>> GetUserProgress()
        {
            try
            {
                var userId = GetUserId();
                var userProgress = await _userProgressRepository.GetUserProgressAsync(userId);
                var userVocabulary = await _userVocabularyRepository.GetUserVocabularyAsync(userId);
                var user = await _userRepository.GetByIdAsync(userId);

                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Eğer progress bulunamazsa yeni oluştur
                if (userProgress == null)
                {
                    userProgress = new UserProgress
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
                }

                var progressDto = new UserProgressDto
                {
                    ConversationsCount = userProgress.TotalConversations,
                    MessagesCount = userProgress.TotalMessages,
                    VocabularyCount = userVocabulary?.ToList().Count ?? 0,
                    QuizzesTaken = userProgress.TotalQuizzesTaken,
                    AverageScore = userProgress.AverageQuizScore,
                    StreakDays = user.CurrentStreak
                };

                return Ok(progressDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user progress");
                return StatusCode(500, "Failed to retrieve user progress");
            }
        }

        [HttpGet("vocabulary")]
        public async Task<ActionResult<IEnumerable<VocabularyItemDto>>> GetUserVocabulary()
        {
            try
            {
                var userId = GetUserId();
                var vocabulary = await _userVocabularyRepository.GetUserVocabularyAsync(userId);

                var vocabularyDtos = new List<VocabularyItemDto>();
                
                foreach (var item in vocabulary)
                {
                    vocabularyDtos.Add(new VocabularyItemDto
                    {
                        Id = item.Id,
                        Word = item.Word,
                        Translation = item.Translation,
                        Level = item.Level,
                        TimesEncountered = item.TimesEncountered,
                        TimesCorrectlyUsed = item.TimesCorrectlyUsed,
                        LastEncounteredAt = item.LastEncounteredAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                        IsMastered = item.IsMastered
                    });
                }

                return Ok(vocabularyDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user vocabulary");
                return StatusCode(500, "Failed to retrieve user vocabulary");
            }
        }

        [HttpPut("profile")]
        public async Task<ActionResult> UpdateUserProfile([FromBody] UpdateProfileDto profileDto)
        {
            try
            {
                var userId = GetUserId();
                var user = await _userRepository.GetByIdAsync(userId);

                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Güncelleme işlemleri
                if (!string.IsNullOrEmpty(profileDto.Username))
                {
                    user.Username = profileDto.Username;
                }

                if (!string.IsNullOrEmpty(profileDto.Email))
                {
                    user.Email = profileDto.Email;
                }

                // Diğer profil bilgilerini eklemek için model güncellemesi gerekebilir
                // Örneğin: user.NativeLanguage = profileDto.NativeLanguage;

                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();

                return Ok(new { message = "Profile updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return StatusCode(500, "Failed to update user profile");
            }
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Invalid user ID in token");
            }
            return userId;
        }
    }
}