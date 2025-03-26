using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using LanguageLearningApp.API.Domain.Entities;
using LanguageLearningApp.API.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LanguageLearningApp.API.Infrastructure.Services
{
    public class LlmService : ILlmService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<LlmService> _logger;
        private readonly string _ollamaEndpoint;

        public LlmService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<LlmService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _ollamaEndpoint = configuration["Ollama:Endpoint"] ?? "http://localhost:11434/api/generate";
        }

        public async Task<string> GetResponseAsync(string userMessage, List<Message> conversationHistory)
        {
            try
            {
                var prompt = BuildConversationPrompt(userMessage, conversationHistory);
                return await CallOllamaApiAsync(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting LLM response");
                return "I'm sorry, I couldn't process your message at the moment. Please try again later.";
            }
        }

        public async Task<string> AnalyzeErrorsAsync(string userMessage)
        {
            try
            {
                var prompt = @$"
                You are an English language tutor. Analyze the following text for grammar, spelling, and vocabulary errors. 
                For each error found, provide the correction and a brief explanation of the rule. 
                Format your response as JSON with 'errors' as an array of objects containing 'errorText', 'correction', and 'explanation'.
                If there are no errors, return an empty array.

                Text to analyze: '{userMessage}'
                ";

                var response = await CallOllamaApiAsync(prompt);

                try
                {
                    JsonDocument.Parse(response);
                    return response;
                }
                catch (JsonException)
                {
                    _logger.LogWarning("LLM returned non-JSON response for error analysis");
                    if (response.Contains("[") && response.Contains("]"))
                    {
                        var startIdx = response.IndexOf("[");
                        var endIdx = response.LastIndexOf("]") + 1;
                        var jsonPart = "{ \"errors\": " + response.Substring(startIdx, endIdx - startIdx) + "}";
                        try
                        {
                            JsonDocument.Parse(jsonPart);
                            return jsonPart;
                        }
                        catch
                        {
                            // Fall back to empty array
                        }
                    }
                    return "{ \"errors\": [] }";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing text for errors");
                return "{ \"errors\": [] }";
            }
        }

        public async Task<List<string>> ExtractVocabularyAsync(string userMessage)
        {
            try
            {
                var prompt = @$"
                Extract the most important vocabulary words from the following text. 
                Include only words that would be useful for an English language learner to know.
                Return a JSON array containing only the words.

                Text: '{userMessage}'
                ";

                var response = await CallOllamaApiAsync(prompt);

                try
                {
                    var words = JsonSerializer.Deserialize<List<string>>(response);
                    return words ?? new List<string>();
                }
                catch (JsonException)
                {
                    if (response.Contains("[") && response.Contains("]"))
                    {
                        try
                        {
                            var startIdx = response.IndexOf("[");
                            var endIdx = response.LastIndexOf("]") + 1;
                            var jsonPart = response.Substring(startIdx, endIdx - startIdx);
                            var wordsFixed = JsonSerializer.Deserialize<List<string>>(jsonPart);
                            return wordsFixed ?? new List<string>();
                        }
                        catch
                        {
                            // Continue to fallback
                        }
                    }
                    
                    // Fallback: extract words from text response
                    return response
                        .Split(new[] { ' ', ',', '.', '\n', '\r', '"', '\'' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(w => w.Length > 2 && !string.IsNullOrWhiteSpace(w))
                        .Take(10)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting vocabulary");
                return new List<string>();
            }
        }

        public async Task<string> GetVocabularyLevelAsync(string word)
        {
            try
            {
                var prompt = @$"
                Determine the CEFR level (A1, A2, B1, B2, C1, or C2) of the English word '{word}'.
                Return only the level designation as a single string.
                ";

                var response = await CallOllamaApiAsync(prompt);

                if (response.Contains("A1")) return "A1";
                if (response.Contains("A2")) return "A2";
                if (response.Contains("B1")) return "B1";
                if (response.Contains("B2")) return "B2";
                if (response.Contains("C1")) return "C1";
                if (response.Contains("C2")) return "C2";

                return "B1"; // Default level if unable to determine
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining vocabulary level");
                return "B1"; // Default level in case of error
            }
        }

        public async Task<string> GenerateQuizQuestionAsync(string topic, string level)
        {
            try
            {
                var prompt = @$"
                You are an English language teacher creating a vocabulary quiz.

                Create ONE multiple-choice question about the word '{topic}' appropriate for {level} level English students.
                
                Format your response as:
                Question: [Write a clear, specific question about the word]
                a) [Correct answer]
                b) [Wrong answer 1]
                c) [Wrong answer 2]
                d) [Wrong answer 3]
                Correct: a
                ";

                var response = await CallOllamaApiAsync(prompt);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating quiz question");
                return $"Question: What does '{topic}' mean?\na) The correct meaning\nb) Wrong meaning 1\nc) Wrong meaning 2\nd) Wrong meaning 3\nCorrect: a";
            }
        }

        public async Task<string> GenerateVocabularyQuizAsync(List<string> words, string level, int questionCount = 5)
        {
            try
            {
                // Create a simple, clear prompt
                string wordList = string.Join(", ", words);
                
                var prompt = @$"
                You are an English language teacher creating a vocabulary quiz with {questionCount} multiple-choice questions.
                
                Use ONLY these words: {wordList}
                
                Create exactly {questionCount} multiple-choice questions suitable for {level} level students.
                
                Format each question with:
                - A clear question
                - 4 options labeled a, b, c, d
                - Make sure the correct answer is included
                
                DO NOT use markdown formatting.
                DO NOT include explanations.
                DO NOT add any introduction or conclusion.
                JUST create the questions and options.
                
                Format your response as:
                Question 1: [Question text]
                a) [Option a]
                b) [Option b]
                c) [Option c]
                d) [Option d]
                Correct: [correct letter]
                
                Question 2: [Question text]
                ...and so on.
                ";

                var response = await CallOllamaApiAsync(prompt);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating vocabulary quiz");
                return "Error generating quiz. Please try again.";
            }
        }

        private string BuildConversationPrompt(string userMessage, List<Message> conversationHistory)
        {
            var sb = new StringBuilder();

            sb.AppendLine("You are an AI English language tutor. You help users learn English by having conversations with them, " +
                         "correcting their grammar and vocabulary errors, and providing helpful explanations. " +
                         "Be friendly, encouraging, and educational.");

            if (conversationHistory.Count > 0)
            {
                sb.AppendLine("\nConversation history:");
                foreach (var message in conversationHistory.TakeLast(10))
                {
                    var role = message.IsFromUser ? "User" : "Tutor";
                    sb.AppendLine($"{role}: {message.Content}");
                }
            }

            sb.AppendLine($"\nUser: {userMessage}");
            sb.AppendLine("\nTutor:");

            return sb.ToString();
        }

        private async Task<string> CallOllamaApiAsync(string prompt)
        {
            var requestBody = new
            {
                model = "gemma3:4b",
                prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = 0.7,
                    top_p = 0.9,
                    max_tokens = 1024
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(_ollamaEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Ollama API error: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(responseJson);

            if (jsonDoc.RootElement.TryGetProperty("response", out var responseElement))
            {
                return responseElement.GetString() ?? string.Empty;
            }

            return string.Empty;
        }
    }
}