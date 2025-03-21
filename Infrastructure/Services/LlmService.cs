using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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
                // Build prompt with conversation history (last 10 messages or fewer)
                var prompt = BuildConversationPrompt(userMessage, conversationHistory);

                // Call Ollama API with Gemma model
                var response = await CallOllamaApiAsync(prompt);

                return response;
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

                // Ensure response is in valid JSON format
                try
                {
                    JsonDocument.Parse(response);
                    return response;
                }
                catch (JsonException)
                {
                    _logger.LogWarning("LLM returned non-JSON response for error analysis. Attempting to format.");
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
                    return JsonSerializer.Deserialize<List<string>>(response) ?? new List<string>();
                }
                catch (JsonException)
                {
                    _logger.LogWarning("LLM returned non-JSON response for vocabulary extraction. Attempting to parse.");
                    return new List<string>();
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

                // Extract just the level (A1, A2, B1, B2, C1, C2)
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
                Generate an English language quiz question about '{topic}' for CEFR level {level}.
                Return the response as JSON with the following properties:
                - question: The question text
                - options: An array of 4 possible answers
                - correctAnswer: The correct option (must be one of the options)
                - explanation: A brief explanation of why this is the correct answer
                ";

                var response = await CallOllamaApiAsync(prompt);

                try
                {
                    JsonDocument.Parse(response);
                    return response;
                }
                catch (JsonException)
                {
                    _logger.LogWarning("LLM returned non-JSON response for quiz generation. Attempting to handle.");
                    return "{ \"question\": \"What is the meaning of 'hello'?\", \"options\": [\"A greeting\", \"A farewell\", \"An expression of surprise\", \"A question\"], \"correctAnswer\": \"A greeting\", \"explanation\": \"'Hello' is a common greeting in English.\" }";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating quiz question");
                return "{ \"question\": \"What is the meaning of 'hello'?\", \"options\": [\"A greeting\", \"A farewell\", \"An expression of surprise\", \"A question\"], \"correctAnswer\": \"A greeting\", \"explanation\": \"'Hello' is a common greeting in English.\" }";
            }
        }

        private string BuildConversationPrompt(string userMessage, List<Message> conversationHistory)
        {
            var sb = new StringBuilder();

            sb.AppendLine("You are an AI English language tutor. You help users learn English by having conversations with them, " +
                         "correcting their grammar and vocabulary errors, and providing helpful explanations. " +
                         "Be friendly, encouraging, and educational.");

            // Add conversation history
            if (conversationHistory.Count > 0)
            {
                sb.AppendLine("\nConversation history:");
                foreach (var message in conversationHistory)
                {
                    var role = message.IsFromUser ? "User" : "Tutor";
                    sb.AppendLine($"{role}: {message.Content}");
                }
            }

            // Add current user message
            sb.AppendLine($"\nUser: {userMessage}");
            sb.AppendLine("\nTutor:");

            return sb.ToString();
        }

        private async Task<string> CallOllamaApiAsync(string prompt)
        {
            var requestBody = new
            {
                // DÜZELTİLDİ: "gemma:3-4b" yerine "gemma3:4b" kullanın (listedeki gibi)
                model = "gemma3:4b", // Ollama list komutunda görünen format
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

            // Extract the generated text
            if (jsonDoc.RootElement.TryGetProperty("response", out var responseElement))
            {
                return responseElement.GetString() ?? string.Empty;
            }

            return string.Empty;
        }
    }
}