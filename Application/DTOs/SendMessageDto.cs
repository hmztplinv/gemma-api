using System.ComponentModel.DataAnnotations;

namespace LanguageLearningApp.API.Application.DTOs
{
    public class SendMessageDto
    {
        [Required]
        public string Content { get; set; }
    }
}