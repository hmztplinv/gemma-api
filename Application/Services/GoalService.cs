using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageLearningApp.API.Application.DTOs;
using LanguageLearningApp.API.Domain.Entities;
using LanguageLearningApp.API.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LanguageLearningApp.API.Application.Services
{
    public class GoalService : IGoalService
    {
        private readonly IUserGoalRepository _userGoalRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GoalService> _logger;

        public GoalService(
            IUserGoalRepository userGoalRepository,
            IUserRepository userRepository,
            ILogger<GoalService> logger)
        {
            _userGoalRepository = userGoalRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<GoalDto>> GetUserGoalsAsync(int userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    throw new KeyNotFoundException($"User with ID {userId} not found");
                }

                var userGoals = await _userGoalRepository.GetUserGoalsAsync(userId);
                
                if (userGoals == null || !userGoals.Any())
                {
                    // PoC için mock veri dönelim
                    return new List<GoalDto>
                    {
                        new GoalDto
                        {
                            Id = 1,
                            Title = "Learn New Vocabulary",
                            TargetType = "words",
                            TargetValue = 10,
                            CurrentProgress = 7,
                            Frequency = "daily",
                            IsCompleted = false,
                            StartDate = DateTime.UtcNow.AddDays(-7),
                            EndDate = DateTime.UtcNow.AddDays(7),
                            Category = "Vocabulary",
                            Progress = 70
                        },
                        new GoalDto
                        {
                            Id = 2,
                            Title = "Practice Conversations",
                            TargetType = "conversations",
                            TargetValue = 5,
                            CurrentProgress = 3,
                            Frequency = "weekly",
                            IsCompleted = false,
                            StartDate = DateTime.UtcNow.AddDays(-10),
                            EndDate = DateTime.UtcNow.AddDays(4),
                            Category = "Conversations",
                            Progress = 60
                        },
                        new GoalDto
                        {
                            Id = 3,
                            Title = "Complete Grammar Quizzes",
                            TargetType = "quizzes",
                            TargetValue = 3,
                            CurrentProgress = 3,
                            Frequency = "weekly",
                            IsCompleted = true,
                            StartDate = DateTime.UtcNow.AddDays(-14),
                            EndDate = DateTime.UtcNow.AddDays(-1),
                            Category = "Quizzes",
                            Progress = 100
                        }
                    };
                }

                return userGoals.Select(MapUserGoalToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving goals for user {userId}");
                throw;
            }
        }

        public async Task<GoalDto> GetUserGoalByIdAsync(int userId, int goalId)
        {
            try
            {
                var userGoal = await _userGoalRepository.GetUserGoalByIdAsync(userId, goalId);
                if (userGoal == null)
                {
                    throw new KeyNotFoundException($"Goal with ID {goalId} not found for user {userId}");
                }

                return MapUserGoalToDto(userGoal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving goal {goalId} for user {userId}");
                throw;
            }
        }

        public async Task<GoalDto> CreateUserGoalAsync(int userId, GoalDto goalDto)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    throw new KeyNotFoundException($"User with ID {userId} not found");
                }

                // Yeni UserGoal entity'si oluştur
                var userGoal = new UserGoal
                {
                    UserId = userId,
                    GoalId = 1, // Default goal ID for now
                    CustomTargetValue = goalDto.TargetValue,
                    CurrentProgress = 0,
                    IsCompleted = false,
                    StartDate = DateTime.UtcNow,
                    EndDate = goalDto.EndDate
                };

                await _userGoalRepository.AddAsync(userGoal);
                await _userGoalRepository.SaveChangesAsync();

                // Kaydedilen entity'yi DTO'ya dönüştür
                goalDto.Id = userGoal.Id;
                goalDto.CurrentProgress = 0;
                goalDto.IsCompleted = false;
                goalDto.StartDate = userGoal.StartDate;
                goalDto.Progress = 0;

                return goalDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating goal for user {userId}");
                throw;
            }
        }

        public async Task<GoalDto> UpdateUserGoalAsync(int userId, int goalId, GoalDto goalDto)
        {
            try
            {
                var userGoal = await _userGoalRepository.GetUserGoalByIdAsync(userId, goalId);
                if (userGoal == null)
                {
                    throw new KeyNotFoundException($"Goal with ID {goalId} not found for user {userId}");
                }

                // Entity'yi güncelle
                userGoal.CustomTargetValue = goalDto.TargetValue;
                userGoal.CurrentProgress = goalDto.CurrentProgress;
                userGoal.IsCompleted = goalDto.IsCompleted;
                userGoal.EndDate = goalDto.EndDate;

                _userGoalRepository.Update(userGoal);
                await _userGoalRepository.SaveChangesAsync();

                return MapUserGoalToDto(userGoal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating goal {goalId} for user {userId}");
                throw;
            }
        }

        public async Task<bool> DeleteUserGoalAsync(int userId, int goalId)
        {
            try
            {
                var userGoal = await _userGoalRepository.GetUserGoalByIdAsync(userId, goalId);
                if (userGoal == null)
                {
                    return false;
                }

                _userGoalRepository.Remove(userGoal);
                return await _userGoalRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting goal {goalId} for user {userId}");
                throw;
            }
        }

        private GoalDto MapUserGoalToDto(UserGoal userGoal)
        {
            int progressPercentage = 0;
            if (userGoal.CustomTargetValue > 0)
            {
                progressPercentage = (int)Math.Min(100, (userGoal.CurrentProgress * 100.0 / userGoal.CustomTargetValue));
            }

            return new GoalDto
            {
                Id = userGoal.Id,
                Title = userGoal.Goal?.Name ?? "Custom Goal",
                TargetType = userGoal.Goal?.TargetType ?? "custom",
                TargetValue = userGoal.CustomTargetValue,
                CurrentProgress = userGoal.CurrentProgress,
                Frequency = userGoal.Goal?.GoalType ?? "daily",
                IsCompleted = userGoal.IsCompleted,
                StartDate = userGoal.StartDate,
                EndDate = userGoal.EndDate,
                Category = userGoal.Goal?.Name ?? "Custom",
                Progress = progressPercentage
            };
        }
    }
}