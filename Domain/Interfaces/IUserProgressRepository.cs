using System.Threading.Tasks;
using LanguageLearningApp.API.Domain.Entities;

namespace LanguageLearningApp.API.Domain.Interfaces
{
    public interface IUserProgressRepository : IRepository<UserProgress>
    {
        Task<UserProgress> GetUserProgressAsync(int userId);
    }
}