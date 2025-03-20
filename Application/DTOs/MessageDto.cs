using System;
using System.Text.Json.Serialization;

namespace LanguageLearningApp.API.Application.DTOs
{
    public class MessageDto
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public bool IsFromUser { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ErrorAnalysisDto ErrorAnalysis { get; set; }
    }
}