// Application/Services/UserVocabularyService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageLearningApp.API.Application.DTOs;
using LanguageLearningApp.API.Application.Interfaces;
using LanguageLearningApp.API.Domain.Entities;
using LanguageLearningApp.API.Domain.Interfaces;

namespace LanguageLearningApp.API.Application.Services
{
    public class UserVocabularyService : IUserVocabularyService
    {
        private readonly IUserVocabularyRepository _userVocabularyRepository;
        private readonly ILlmService _llmService;

        public UserVocabularyService(
            IUserVocabularyRepository userVocabularyRepository,
            ILlmService llmService)
        {
            _userVocabularyRepository = userVocabularyRepository;
            _llmService = llmService;
        }

        public async Task<IEnumerable<VocabularyItemDto>> GetUserVocabularyAsync(int userId)
        {
            var vocabulary = await _userVocabularyRepository.GetUserVocabularyAsync(userId);
            var result = new List<VocabularyItemDto>();

            foreach (var item in vocabulary)
            {
                result.Add(new VocabularyItemDto
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

            return result;
        }

        public async Task<VocabularyItemDto> UpdateVocabularyItemAsync(int userId, int wordId, UpdateVocabularyItemDto updateDto)
        {
            var vocabularyItem = await _userVocabularyRepository.GetByIdAsync(wordId);
            
            if (vocabularyItem == null || vocabularyItem.UserId != userId)
                throw new UnauthorizedAccessException("Vocabulary item not found or access denied");

            // Update properties
            if (!string.IsNullOrEmpty(updateDto.Translation))
                vocabularyItem.Translation = updateDto.Translation;
                
            if (updateDto.IsMastered.HasValue)
                vocabularyItem.IsMastered = updateDto.IsMastered.Value;

            _userVocabularyRepository.Update(vocabularyItem);
            await _userVocabularyRepository.SaveChangesAsync();

            return new VocabularyItemDto
            {
                Id = vocabularyItem.Id,
                Word = vocabularyItem.Word,
                Translation = vocabularyItem.Translation,
                Level = vocabularyItem.Level,
                TimesEncountered = vocabularyItem.TimesEncountered,
                TimesCorrectlyUsed = vocabularyItem.TimesCorrectlyUsed,
                LastEncounteredAt = vocabularyItem.LastEncounteredAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                IsMastered = vocabularyItem.IsMastered
            };
        }

        public async Task<IEnumerable<VocabularyItemDto>> GetFlashcardsAsync(int userId, string level = null, int count = 10)
        {
            var vocabulary = await _userVocabularyRepository.GetUserVocabularyAsync(userId);
            var filteredList = vocabulary.ToList();

            // Filter by level if specified
            if (!string.IsNullOrEmpty(level) && level.ToUpper() != "ALL")
            {
                filteredList = filteredList.Where(v => v.Level == level.ToUpper()).ToList();
            }

            // Prioritize words that are not mastered and less frequently encountered
            filteredList = filteredList
                .OrderBy(v => v.IsMastered)
                .ThenBy(v => v.TimesEncountered)
                .Take(count)
                .ToList();

            // Random shuffle
            Random rnd = new Random();
            filteredList = filteredList.OrderBy(x => rnd.Next()).ToList();

            var result = new List<VocabularyItemDto>();
            foreach (var item in filteredList)
            {
                result.Add(new VocabularyItemDto
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

            return result;
        }
    }
}