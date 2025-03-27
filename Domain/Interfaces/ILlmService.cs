using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageLearningApp.API.Domain.Entities;

namespace LanguageLearningApp.API.Domain.Interfaces
{
    public interface ILlmService
    {
        /// <summary>
        /// Gets an AI response to a user message considering conversation history
        /// </summary>
        Task<string> GetResponseAsync(string userMessage, List<Message> conversationHistory);

        /// <summary>
        /// Analyzes a user message for grammar, spelling, and vocabulary errors
        /// </summary>
        Task<string> AnalyzeErrorsAsync(string userMessage);

        /// <summary>
        /// Extracts vocabulary words from a user message
        /// </summary>
        Task<List<string>> ExtractVocabularyAsync(string userMessage);

        /// <summary>
        /// Determines the CEFR level of a vocabulary word
        /// </summary>
        Task<string> GetVocabularyLevelAsync(string word);

        /// <summary>
        /// Generates a single quiz question for a specific word
        /// </summary>
        Task<string> GenerateQuizQuestionAsync(string word, string level);

        /// <summary>
        /// Generates multiple quiz questions for a list of vocabulary words
        /// </summary>
        Task<List<QuizQuestionData>> GenerateVocabularyQuizAsync(List<string> words, string level, int questionCount = 5);
    }

    // Helper class for deserializing quiz question data from LLM
    public class QuizQuestionData
    {
        public string Question { get; set; }
        public string[] Options { get; set; }
        public string CorrectAnswer { get; set; }
        public string Explanation { get; set; }
    }
}