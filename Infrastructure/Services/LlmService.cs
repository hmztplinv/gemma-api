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
                // Build prompt with conversation history
                var prompt = BuildConversationPrompt(userMessage, conversationHistory);

                // Call Ollama API with model
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
                _logger.LogInformation("Starting vocabulary extraction for message");

                var prompt = @$"
        Extract the most important vocabulary words from the following text. 
        Include only words that would be useful for an English language learner to know.
        Return a JSON array containing only the words.

        Text: '{userMessage}'
        ";

                _logger.LogInformation($"Sending prompt to LLM: {prompt}");

                var response = await CallOllamaApiAsync(prompt);
                _logger.LogInformation($"Raw response from LLM: {response}");

                try
                {
                    var words = JsonSerializer.Deserialize<List<string>>(response);
                    _logger.LogInformation($"Successfully deserialized JSON response: {JsonSerializer.Serialize(words)}");
                    return words ?? new List<string>();
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogWarning($"LLM returned non-JSON response for vocabulary extraction: {jsonEx.Message}");
                    _logger.LogWarning($"Attempting to fix response: {response}");

                    // Basit bir düzeltme denemesi
                    if (response.Contains("[") && response.Contains("]"))
                    {
                        try
                        {
                            var startIdx = response.IndexOf("[");
                            var endIdx = response.LastIndexOf("]") + 1;
                            var jsonPart = response.Substring(startIdx, endIdx - startIdx);
                            _logger.LogInformation($"Extracted JSON part: {jsonPart}");

                            var wordsFixed = JsonSerializer.Deserialize<List<string>>(jsonPart);
                            _logger.LogInformation($"Fixed JSON parse successful: {JsonSerializer.Serialize(wordsFixed)}");
                            return wordsFixed ?? new List<string>();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Fix attempt failed: {ex.Message}");
                        }
                    }

                    return new List<string>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting vocabulary: {ErrorMessage}", ex.Message);
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

        public async Task<string> GenerateQuizQuestionAsync(string word, string level)
{
    try
    {
        _logger.LogInformation($"Generating quiz question for word: '{word}' at level: {level}");

        var prompt = @$"
        You are an English language teacher creating a vocabulary quiz.

        Create ONE high-quality multiple-choice question about the word '{word}' appropriate for {level} level English students.

        EXAMPLES:
        - Word: 'book'
          Question: 'Which of these activities do you typically do with a book?'
          Options: ['Read it', 'Eat it', 'Wear it', 'Drive it']
          CorrectAnswer: 'Read it'

        - Word: 'apple'
          Question: 'What category does an apple belong to?'
          Options: ['Fruit', 'Vegetable', 'Meat', 'Furniture']
          CorrectAnswer: 'Fruit'

        IMPORTANT: Your response must be ONLY a valid JSON object exactly in this format:
        {{
        ""question"": ""[A specific question that tests understanding of the word '{word}']"",
        ""options"": [""[Correct answer]"", ""[Wrong answer 1]"", ""[Wrong answer 2]"", ""[Wrong answer 3]""],
        ""correctAnswer"": ""[Exact copy of the correct answer from options]"",
        ""explanation"": ""[Brief explanation why this is correct]""
        }}

        DO NOT include any markdown formatting like ```json or ``` around your response. Return ONLY the raw JSON object.
        ";

        var response = await CallOllamaApiAsync(prompt);
        _logger.LogInformation($"Quiz question raw response: {response}");

        // Markdown kod bloğu temizleme
        var cleanedResponse = CleanJsonResponse(response);
        _logger.LogInformation($"Cleaned response: {cleanedResponse}");

        try
        {
            var jsonDoc = JsonSerializer.Deserialize<QuizQuestionData>(cleanedResponse);
            
            // Ek doğrulama
            if (jsonDoc != null && 
                !string.IsNullOrEmpty(jsonDoc.Question) && 
                jsonDoc.Options != null && 
                jsonDoc.Options.Length >= 2 && 
                !string.IsNullOrEmpty(jsonDoc.CorrectAnswer))
            {
                _logger.LogInformation("Quiz question generated with valid JSON format");
                return cleanedResponse;
            }
            else
            {
                _logger.LogWarning("JSON validation failed for quiz question");
                throw new JsonException("Required fields missing from quiz question JSON");
            }
        }
        catch (JsonException jsonEx)
        {
            _logger.LogWarning($"LLM returned invalid JSON response for quiz generation: {jsonEx.Message}");
            _logger.LogWarning("Attempting to provide default question format");
            
            return $@"{{ 
                ""question"": ""Which of these best describes the word '{word}'?"", 
                ""options"": [""A common English word"", ""A rare technical term"", ""A fictional concept"", ""A mathematical formula""], 
                ""correctAnswer"": ""A common English word"", 
                ""explanation"": ""'{word}' is a standard word in English vocabulary.""
            }}";
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Error generating quiz question for word: '{word}'");
        return $@"{{ 
            ""question"": ""What is the word '{word}' most commonly used for?"", 
            ""options"": [""Communication"", ""Calculation"", ""Construction"", ""Cooking""], 
            ""correctAnswer"": ""Communication"", 
            ""explanation"": ""Most English words are used primarily for communication.""
        }}";
    }
}

// Markdown kod bloklarını temizleyen yardımcı metod
private string CleanJsonResponse(string response)
{
    // JSON kod bloklarını temizle: ```json ve ``` işaretlerini kaldır
    if (response.StartsWith("```json") || response.StartsWith("```JSON"))
    {
        var startIndex = response.IndexOf('{');
        var endIndex = response.LastIndexOf('}');
        
        if (startIndex >= 0 && endIndex >= 0 && endIndex > startIndex)
        {
            return response.Substring(startIndex, endIndex - startIndex + 1);
        }
    }
    else if (response.Contains("```") && response.Contains("{") && response.Contains("}"))
    {
        // Alternatif temizleme stratejisi - ilk { ve son } arasını al
        var startIndex = response.IndexOf('{');
        var endIndex = response.LastIndexOf('}');
        
        if (startIndex >= 0 && endIndex >= 0 && endIndex > startIndex)
        {
            return response.Substring(startIndex, endIndex - startIndex + 1);
        }
    }
    
    return response; // Eğer kod bloğu formatı yoksa, orijinal yanıtı döndür
}

        public async Task<List<QuizQuestionData>> GenerateVocabularyQuizAsync(List<string> words, string level, int questionCount = 10)
        {
            _logger.LogInformation($"Generating vocabulary quiz with {words.Count} words at level {level}");
            var result = new List<QuizQuestionData>();
            
            // Limit to requested question count
            var wordsToUse = words.Take(Math.Min(words.Count, questionCount)).ToList();
            
            foreach (var word in wordsToUse)
            {
                try
                {
                    var questionJson = await GenerateQuizQuestionAsync(word, level);
                    _logger.LogInformation($"Generated question for word '{word}': {questionJson}");
                    
                    try
                    {
                        var questionData = JsonSerializer.Deserialize<QuizQuestionData>(questionJson);
                        if (questionData != null && 
                            !string.IsNullOrEmpty(questionData.Question) && 
                            questionData.Options != null && 
                            questionData.Options.Length >= 2 && 
                            !string.IsNullOrEmpty(questionData.CorrectAnswer))
                        {
                            result.Add(questionData);
                        }
                        else
                        {
                            _logger.LogWarning($"Invalid question data for word '{word}', using default format");
                            
                            // Create a fallback question if deserialization produces incomplete data
                            result.Add(new QuizQuestionData
                            {
                                Question = $"Which of these best describes the word '{word}'?",
                                Options = new string[] { "A common English word", "A rare medical term", "A type of food", "A place name" },
                                CorrectAnswer = "A common English word",
                                Explanation = $"The word '{word}' is a standard part of English vocabulary."
                            });
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, $"Failed to parse question JSON for word '{word}'");
                        
                        // Create a fallback question if JSON parsing fails
                        result.Add(new QuizQuestionData
                        {
                            Question = $"What type of word is '{word}'?",
                            Options = new string[] { "Noun", "Verb", "Adjective", "Adverb" },
                            CorrectAnswer = "Noun", // Default to noun as safest guess
                            Explanation = $"This is a default question format for the word '{word}'."
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error generating question for word '{word}'");
                }
            }
            
            _logger.LogInformation($"Successfully generated {result.Count} questions for vocabulary quiz");
            return result;
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
            try
            {
                var requestBody = new
                {
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

                _logger.LogDebug($"Sending request to Ollama API: {_ollamaEndpoint}");
                var response = await _httpClient.PostAsync(_ollamaEndpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Ollama API error: {response.StatusCode}, {errorContent}");
                    throw new Exception($"Ollama API error: {response.StatusCode}, {errorContent}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                _logger.LogDebug($"Ollama API responded with: {responseJson}");
                
                using var jsonDoc = JsonDocument.Parse(responseJson);

                // Extract the generated text
                if (jsonDoc.RootElement.TryGetProperty("response", out var responseElement))
                {
                    return responseElement.GetString() ?? string.Empty;
                }

                _logger.LogWarning("Response property not found in Ollama API response");
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CallOllamaApiAsync");
                throw;
            }
        }
    }
}