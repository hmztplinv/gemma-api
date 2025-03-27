using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageLearningApp.API.Domain.Entities;

namespace LanguageLearningApp.API.Domain.Interfaces
{
    public interface IBadgeRepository : IRepository<Badge>
    {
        Task<IEnumerable<Badge>> GetAllBadgesAsync();
    }
}