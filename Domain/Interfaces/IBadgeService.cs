using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageLearningApp.API.Application.DTOs;

namespace LanguageLearningApp.API.Domain.Interfaces
{
    public interface IBadgeService
    {
        Task<IEnumerable<BadgeDto>> GetUserBadgesAsync(int userId);
        Task<BadgeDto> GetUserBadgeByIdAsync(int userId, int badgeId);
    }
}