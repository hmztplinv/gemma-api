using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageLearningApp.API.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LanguageLearningApp.API.Application.Services
{
    public class ErrorAnalysisService : IErrorAnalysisService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<ErrorAnalysisService> _logger;

        public ErrorAnalysisService(
            IUserRepository userRepository,
            ILogger<ErrorAnalysisService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<object> GetUserErrorAnalysisAsync(int userId, string timeRange)
        {
            try
            {
                // Kullanıcının varlığını kontrol et
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    throw new KeyNotFoundException($"User with ID {userId} not found");
                }

                // Zaman aralığına göre filtreleme
                DateTime startDate;
                DateTime endDate = DateTime.UtcNow;

                switch (timeRange.ToLower())
                {
                    case "week":
                        startDate = endDate.AddDays(-7);
                        break;
                    case "month":
                        startDate = endDate.AddMonths(-1);
                        break;
                    case "year":
                        startDate = endDate.AddYears(-1);
                        break;
                    case "all":
                        startDate = DateTime.MinValue;
                        break;
                    default:
                        startDate = endDate.AddMonths(-1); // Varsayılan: son 1 ay
                        break;
                }

                // Burada veritabanından gerçek hata verileri alınmalı
                // Şimdilik mock veri döndürelim
                var errorAnalysis = new
                {
                    totalErrors = 157,
                    errorCategories = new[]
                    {
                        new { name = "Grammar", count = 68 },
                        new { name = "Vocabulary", count = 42 },
                        new { name = "Spelling", count = 27 },
                        new { name = "Punctuation", count = 13 },
                        new { name = "Word Order", count = 7 }
                    },
                    topErrorTypes = new[]
                    {
                        new { name = "Subject-Verb Agreement", count = 24, category = "Grammar" },
                        new { name = "Article Usage", count = 19, category = "Grammar" },
                        new { name = "Word Choice", count = 17, category = "Vocabulary" },
                        new { name = "Verb Tense", count = 15, category = "Grammar" },
                        new { name = "Preposition Usage", count = 12, category = "Grammar" }
                    },
                    monthlyTrends = new[]
                    {
                        new { month = "Jan", errors = 42, corrections = 38 },
                        new { month = "Feb", errors = 38, corrections = 35 },
                        new { month = "Mar", errors = 31, corrections = 30 },
                        new { month = "Apr", errors = 28, corrections = 27 },
                        new { month = "May", errors = 18, corrections = 18 }
                    },
                    errorImprovement = new
                    {
                        previousPeriod = 87,
                        currentPeriod = 70,
                        percentageChange = -19.5
                    }
                };

                return errorAnalysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing user errors for user {userId}");
                throw;
            }
        }
    }
}