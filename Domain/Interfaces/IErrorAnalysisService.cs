using System.Threading.Tasks;

namespace LanguageLearningApp.API.Domain.Interfaces
{
    public interface IErrorAnalysisService
    {
        Task<object> GetUserErrorAnalysisAsync(int userId, string timeRange);
    }
}