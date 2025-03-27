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

        // İyileştirilmiş JSON temizleme fonksiyonu
        private string CleanJsonResponse(string response)
        {
            _logger.LogDebug($"Cleaning JSON response: {response}");
            
            // Tüm yaygın başlangıç ve bitiş işaretlerini temizle
            string[] unwantedPrefixes = new[] { "```json", "```", "JSON:" };
            string[] unwantedSuffixes = new[] { "```" };
            
            string cleaned = response;
            
            // Başlangıç temizleme
            foreach (var prefix in unwantedPrefixes)
            {
                if (cleaned.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = cleaned.Substring(prefix.Length).Trim();
                }
            }
            
            // Bitiş temizleme
            foreach (var suffix in unwantedSuffixes)
            {
                if (cleaned.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = cleaned.Substring(0, cleaned.Length - suffix.Length).Trim();
                }
            }
            
            // JSON içeriğin başlangıç ve bitiş noktalarını bul
            var startIdx = cleaned.IndexOf('{');
            var endIdx = cleaned.LastIndexOf('}');
            
            if (startIdx >= 0 && endIdx > startIdx)
            {
                cleaned = cleaned.Substring(startIdx, endIdx - startIdx + 1);
                _logger.LogDebug($"Extracted JSON object: {cleaned}");
                return cleaned;
            }
            
            _logger.LogWarning("Could not find valid JSON object in response");
            return response; // Eğer düzgün JSON bulunamazsa, orijinal yanıtı döndür
        }

        public async Task<string> GenerateQuizQuestionAsync(string word, string level)
        {
            try
            {
                _logger.LogInformation($"Generating quiz question for word: '{word}' at level: {level}");

                // İyileştirilmiş prompt - kesin format talimatları ile
                var prompt = @$"
You are an English language teacher creating a vocabulary quiz for a {level} level student.

Create ONE multiple-choice question about the word '{word}'. 

Your response MUST be a valid JSON object in this EXACT format:
{{
  ""question"": ""Which of these best describes the word '{word}'?"",
  ""options"": [""[Correct definition]"", ""[Example sentence]"", ""[Plausible incorrect definition]"", ""[Clearly wrong definition]""],
  ""correctAnswer"": ""[Correct definition]"",
  ""explanation"": ""[Brief explanation of the word meaning]""
}}

The correctAnswer MUST exactly match one of the options.
DO NOT include any text before or after the JSON.
DO NOT wrap the JSON in code blocks (```).
Return ONLY the raw JSON object.
";

                var response = await CallOllamaApiAsync(prompt);
                _logger.LogInformation($"Raw LLM response for word '{word}': {response}");

                var cleanedResponse = CleanJsonResponse(response);
                _logger.LogInformation($"Cleaned JSON response for word '{word}': {cleanedResponse}");

                // JSON doğrulaması yap
                try 
                {
                    // Deserialize edilebiliyorsa geçerli JSON'dur
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var questionData = JsonSerializer.Deserialize<QuizQuestionData>(cleanedResponse, options);
                    
                    // Gerekli alanlar var mı kontrol et
                    if (questionData?.Question == null || questionData?.Options == null || 
                        questionData.Options.Length < 2 || string.IsNullOrEmpty(questionData.CorrectAnswer))
                    {
                        _logger.LogWarning($"JSON structure is valid but content is incomplete for word '{word}'");
                        throw new JsonException("Missing required fields in JSON");
                    }
                    
                    // CorrectAnswer seçeneklerden biri mi?
                    bool correctAnswerInOptions = false;
                    foreach (var option in questionData.Options)
                    {
                        if (option.Equals(questionData.CorrectAnswer, StringComparison.OrdinalIgnoreCase))
                        {
                            correctAnswerInOptions = true;
                            break;
                        }
                    }
                    
                    if (!correctAnswerInOptions)
                    {
                        _logger.LogWarning($"CorrectAnswer not found in options for word '{word}'");
                        throw new JsonException("CorrectAnswer must match one of the options");
                    }
                    
                    _logger.LogInformation($"JSON validation successful for word '{word}'");
                    return cleanedResponse;
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning($"Invalid JSON for word '{word}': {ex.Message}");
                    
                    // JSON geçersizse, düzgün formatlı bir fallback oluştur
                    var fallbackData = new QuizQuestionData
                    {
                        Question = $"Which of these best describes the word '{word}'?",
                        Options = new string[] 
                        { 
                            $"The correct meaning of '{word}'", 
                            $"A sentence using '{word}'", 
                            $"A related concept to '{word}'", 
                            $"An unrelated concept" 
                        },
                        CorrectAnswer = $"The correct meaning of '{word}'",
                        Explanation = $"This question tests your understanding of the word '{word}'."
                    };
                    
                    var fallbackJson = JsonSerializer.Serialize(fallbackData);
                    _logger.LogInformation($"Created fallback JSON for word '{word}': {fallbackJson}");
                    return fallbackJson;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating quiz question for word: '{word}'");
                throw;
            }
        }

        public async Task<List<QuizQuestionData>> GenerateVocabularyQuizAsync(List<string> words, string level, int questionCount = 3)
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
                    _logger.LogInformation($"Question JSON generated for word '{word}'");

                    try
                    {
                        var options = new JsonSerializerOptions 
                        { 
                            PropertyNameCaseInsensitive = true,
                            AllowTrailingCommas = true
                        };
                        
                        var questionData = JsonSerializer.Deserialize<QuizQuestionData>(questionJson, options);
                        
                        if (questionData != null)
                        {
                            // Seçenekleri karıştır ve doğru cevabı takip et
                            ShuffleOptions(questionData);
                            
                            // Deserialize başarılı olduysa ekle
                            result.Add(questionData);
                            _logger.LogInformation($"Successfully added question for word '{word}'");
                        }
                        else
                        {
                            throw new JsonException("Deserialized to null object");
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, $"JSON parsing error for word '{word}'");
                        
                        // Bu durumda gerçekten anlamlı bir fallback soru kullan
                        var fallbackData = new QuizQuestionData
                        {
                            Question = $"Which of these best describes the word '{word}'?",
                            Options = new string[] 
                            { 
                                $"A word commonly used in {level} level English", 
                                "A rare technical term", 
                                "A type of grammatical structure", 
                                "A literary device" 
                            },
                            CorrectAnswer = $"A word commonly used in {level} level English",
                            Explanation = $"'{word}' is a vocabulary word typically taught at {level} level."
                        };
                        
                        // Fallback sorularında da seçenekleri karıştır
                        ShuffleOptions(fallbackData);
                        result.Add(fallbackData);
                        _logger.LogInformation($"Added fallback question for word '{word}'");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Processing error for word '{word}'");
                    // Bu kelimeyi atla ve devam et
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

        // Seçenekleri karıştırıp doğru cevabı güncelleyen yardımcı fonksiyon
private void ShuffleOptions(QuizQuestionData questionData)
{
    if (questionData?.Options == null || questionData.Options.Length < 2)
        return;
        
    // Doğru cevabı bul ve indeksini kaydet
    string correctAnswer = questionData.CorrectAnswer;
    
    // Seçenekleri karıştır
    Random random = new Random();
    string[] shuffledOptions = questionData.Options.OrderBy(x => random.Next()).ToArray();
    
    // Karıştırılmış dizide doğru cevabın yerini bul
    int newCorrectIndex = Array.FindIndex(shuffledOptions, option => option.Equals(correctAnswer));
    if (newCorrectIndex != -1)
    {
        // Options ve CorrectAnswer'ı güncelle
        questionData.Options = shuffledOptions;
        questionData.CorrectAnswer = shuffledOptions[newCorrectIndex];
        _logger.LogDebug($"Options shuffled, correct answer is now at position {newCorrectIndex}");
    }
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