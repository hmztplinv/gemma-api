// Application/Interfaces/IUserVocabularyService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageLearningApp.API.Application.DTOs;

namespace LanguageLearningApp.API.Application.Interfaces
{
    public interface IUserVocabularyService
    {
        Task<IEnumerable<VocabularyItemDto>> GetUserVocabularyAsync(int userId);
        Task<VocabularyItemDto> UpdateVocabularyItemAsync(int userId, int wordId, UpdateVocabularyItemDto updateDto);
        Task<IEnumerable<VocabularyItemDto>> GetFlashcardsAsync(int userId, string level = null, int count = 10);
    }
}