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
        private readonly IErrorAnalysisService _errorAnalysisService;
        private readonly IGoalService _goalService;
        private readonly IBadgeService _badgeService;
        

        public UsersController(
            IUserRepository userRepository,
            IUserVocabularyRepository userVocabularyRepository,
            IUserProgressRepository userProgressRepository,
            ILogger<UsersController> logger,
            IErrorAnalysisService errorAnalysisService,
            IGoalService goalService,
            IBadgeService badgeService
            )
        {
            _userRepository = userRepository;
            _userVocabularyRepository = userVocabularyRepository;
            _userProgressRepository = userProgressRepository;
            _logger = logger;
            _errorAnalysisService = errorAnalysisService;
            _goalService = goalService;
            _badgeService = badgeService;
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

        // 1. Error Analysis için endpoint
        [HttpGet("errors")]
public async Task<ActionResult<object>> GetUserErrorAnalysis([FromQuery] string timeRange = "month")
{
    try
    {
        var userId = GetUserId();
        var result = await _errorAnalysisService.GetUserErrorAnalysisAsync(userId, timeRange);
        return Ok(result);
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(ex.Message);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving error analysis");
        return StatusCode(500, "Error retrieving error analysis");
    }
}

[HttpGet("goals")]
public async Task<ActionResult<IEnumerable<GoalDto>>> GetUserGoals()
{
    try
    {
        var userId = GetUserId();
        var goals = await _goalService.GetUserGoalsAsync(userId);
        return Ok(goals);
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(ex.Message);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving goals");
        return StatusCode(500, "Error retrieving goals");
    }
}

[HttpGet("goals/{id}")]
public async Task<ActionResult<GoalDto>> GetUserGoalById(int id)
{
    try
    {
        var userId = GetUserId();
        var goal = await _goalService.GetUserGoalByIdAsync(userId, id);
        return Ok(goal);
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(ex.Message);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Error retrieving goal {id}");
        return StatusCode(500, $"Error retrieving goal {id}");
    }
}

[HttpPost("goals")]
public async Task<ActionResult<GoalDto>> CreateUserGoal([FromBody] GoalDto goalDto)
{
    try
    {
        var userId = GetUserId();
        var goal = await _goalService.CreateUserGoalAsync(userId, goalDto);
        return CreatedAtAction(nameof(GetUserGoalById), new { id = goal.Id }, goal);
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(ex.Message);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating goal");
        return StatusCode(500, "Error creating goal");
    }
}

[HttpPut("goals/{id}")]
public async Task<ActionResult<GoalDto>> UpdateUserGoal(int id, [FromBody] GoalDto goalDto)
{
    try
    {
        var userId = GetUserId();
        goalDto.Id = id; // ID'yi DTO içerisine ata
        var goal = await _goalService.UpdateUserGoalAsync(userId, id, goalDto);
        return Ok(goal);
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(ex.Message);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Error updating goal {id}");
        return StatusCode(500, $"Error updating goal {id}");
    }
}

[HttpDelete("goals/{id}")]
public async Task<ActionResult> DeleteUserGoal(int id)
{
    try
    {
        var userId = GetUserId();
        var result = await _goalService.DeleteUserGoalAsync(userId, id);
        if (!result)
        {
            return NotFound($"Goal with ID {id} not found");
        }
        return NoContent();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Error deleting goal {id}");
        return StatusCode(500, $"Error deleting goal {id}");
    }
}

[HttpGet("badges")]
public async Task<ActionResult<IEnumerable<BadgeDto>>> GetUserBadges()
{
    try
    {
        var userId = GetUserId();
        var badges = await _badgeService.GetUserBadgesAsync(userId);
        return Ok(badges);
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(ex.Message);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving badges");
        return StatusCode(500, "Error retrieving badges");
    }
}

[HttpGet("badges/{id}")]
public async Task<ActionResult<BadgeDto>> GetUserBadgeById(int id)
{
    try
    {
        var userId = GetUserId();
        var badge = await _badgeService.GetUserBadgeByIdAsync(userId, id);
        return Ok(badge);
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(ex.Message);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Error retrieving badge {id}");
        return StatusCode(500, $"Error retrieving badge {id}");
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