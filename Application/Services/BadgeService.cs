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
    public class BadgeService : IBadgeService
    {
        private readonly IUserBadgeRepository _userBadgeRepository;
        private readonly IBadgeRepository _badgeRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUserProgressRepository _userProgressRepository;
        private readonly ILogger<BadgeService> _logger;

        public BadgeService(
            IUserBadgeRepository userBadgeRepository,
            IBadgeRepository badgeRepository,
            IUserRepository userRepository,
            IUserProgressRepository userProgressRepository,
            ILogger<BadgeService> logger)
        {
            _userBadgeRepository = userBadgeRepository;
            _badgeRepository = badgeRepository;
            _userRepository = userRepository;
            _userProgressRepository = userProgressRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<BadgeDto>> GetUserBadgesAsync(int userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    throw new KeyNotFoundException($"User with ID {userId} not found");
                }

                // Bu kullanıcının kazandığı rozetleri al
                var userBadges = await _userBadgeRepository.GetUserBadgesAsync(userId);
                
                // Kullanıcı ilerleme verileri
                var userProgress = await _userProgressRepository.GetUserProgressAsync(userId);
                
                // Tüm mevcut rozetler
                var allBadges = await _badgeRepository.GetAllBadgesAsync();

                // Eğer veri yoksa
                if (!allBadges.Any())
                {
                    // Mock veriler oluştur
                    return new List<BadgeDto>
                    {
                        new BadgeDto
                        {
                            Id = 1,
                            Name = "First Conversation",
                            Description = "Complete your first conversation with the AI tutor.",
                            ImageUrl = "🗣️",
                            Category = "Conversations",
                            EarnedAt = DateTime.UtcNow.AddDays(-20),
                            IsEarned = true,
                            Progress = 100
                        },
                        new BadgeDto
                        {
                            Id = 2,
                            Name = "Vocabulary Builder",
                            Description = "Learn 50 new words.",
                            ImageUrl = "📚",
                            Category = "Vocabulary",
                            EarnedAt = DateTime.UtcNow.AddDays(-15),
                            IsEarned = true,
                            Progress = 100
                        },
                        new BadgeDto
                        {
                            Id = 3,
                            Name = "Quiz Master",
                            Description = "Score 90% or higher on 5 quizzes.",
                            ImageUrl = "🏆",
                            Category = "Quizzes",
                            EarnedAt = null,
                            IsEarned = false,
                            Progress = 60
                        },
                        new BadgeDto
                        {
                            Id = 4,
                            Name = "Perfect Streak",
                            Description = "Maintain a 7-day learning streak.",
                            ImageUrl = "🔥",
                            Category = "Engagement",
                            EarnedAt = DateTime.UtcNow.AddDays(-10),
                            IsEarned = true,
                            Progress = 100
                        }
                    };
                }

                // Gerçek veri ile DTO'ları oluştur
                var badgeDtos = new List<BadgeDto>();

                foreach (var badge in allBadges)
                {
                    var userBadge = userBadges.FirstOrDefault(ub => ub.BadgeId == badge.Id);
                    var isEarned = userBadge != null;

                    // İlerleme hesaplama
                    int progress = 0;
                    if (isEarned)
                    {
                        progress = 100;
                    }
                    else
                    {
                        // Kullanıcının bu rozete doğru ilerlemesini hesaplayalım
                        // Bu örnekte basit bir hesaplama yapılıyor
                        progress = CalculateBadgeProgress(badge, userProgress);
                    }

                    badgeDtos.Add(new BadgeDto
                    {
                        Id = badge.Id,
                        Name = badge.Name,
                        Description = badge.Description,
                        ImageUrl = badge.ImageUrl,
                        Category = badge.RequirementType,
                        EarnedAt = userBadge?.AchievedAt,
                        IsEarned = isEarned,
                        Progress = progress
                    });
                }

                return badgeDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving badges for user {userId}");
                throw;
            }
        }

        public async Task<BadgeDto> GetUserBadgeByIdAsync(int userId, int badgeId)
        {
            try
            {
                var badge = await _badgeRepository.GetByIdAsync(badgeId);
                if (badge == null)
                {
                    throw new KeyNotFoundException($"Badge with ID {badgeId} not found");
                }

                var userBadge = await _userBadgeRepository.GetUserBadgeByIdAsync(userId, badgeId);
                var isEarned = userBadge != null;

                // İlerleme hesaplama
                int progress = 0;
                if (isEarned)
                {
                    progress = 100;
                }
                else
                {
                    // Kullanıcının bu rozete doğru ilerlemesini hesaplayalım
                    var userProgress = await _userProgressRepository.GetUserProgressAsync(userId);
                    progress = CalculateBadgeProgress(badge, userProgress);
                }

                return new BadgeDto
                {
                    Id = badge.Id,
                    Name = badge.Name,
                    Description = badge.Description,
                    ImageUrl = badge.ImageUrl,
                    Category = badge.RequirementType,
                    EarnedAt = userBadge?.AchievedAt,
                    IsEarned = isEarned,
                    Progress = progress
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving badge {badgeId} for user {userId}");
                throw;
            }
        }

        private int CalculateBadgeProgress(Badge badge, UserProgress userProgress)
        {
            if (userProgress == null) return 0;

            int currentValue = 0;
            int targetValue = badge.RequirementValue;

            switch (badge.RequirementType.ToLower())
            {
                case "words":
                    currentValue = userProgress.TotalWordsLearned;
                    break;
                case "quizzes":
                    currentValue = userProgress.TotalQuizzesTaken;
                    break;
                case "conversations":
                    currentValue = userProgress.TotalConversations;
                    break;
                case "streak":
                    // Burada dinamik olarak kullanıcının streak'i kontrol edilmeli
                    break;
                default:
                    return 0;
            }

            return Math.Min(100, (int)(currentValue * 100.0 / targetValue));
        }
    }
}