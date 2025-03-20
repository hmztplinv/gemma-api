using System.ComponentModel.DataAnnotations;

namespace LanguageLearningApp.API.Application.DTOs
{
    public class CreateConversationDto
    {
        [Required]
        public string Title { get; set; }
        
        [Required]
        public string InitialMessage { get; set; }
    }
}