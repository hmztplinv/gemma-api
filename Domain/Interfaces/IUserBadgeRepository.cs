using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageLearningApp.API.Domain.Entities;

namespace LanguageLearningApp.API.Domain.Interfaces
{
    public interface IUserBadgeRepository : IRepository<UserBadge>
    {
        Task<IEnumerable<UserBadge>> GetUserBadgesAsync(int userId);
        Task<UserBadge> GetUserBadgeByIdAsync(int userId, int badgeId);
    }
}