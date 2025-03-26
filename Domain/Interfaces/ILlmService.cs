using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageLearningApp.API.Domain.Entities;

namespace LanguageLearningApp.API.Domain.Interfaces
{
    public interface ILlmService
    {
        Task<string> GetResponseAsync(string userMessage, List<Message> conversationHistory);
        Task<string> AnalyzeErrorsAsync(string userMessage);
        Task<List<string>> ExtractVocabularyAsync(string userMessage);
        Task<string> GetVocabularyLevelAsync(string word);
        Task<string> GenerateQuizQuestionAsync(string topic, string level);
        
        // QuizService'in çağırdığı metod
        Task<string> GenerateVocabularyQuizAsync(List<string> words, string level, int questionCount = 5);
    }
}