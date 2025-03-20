using System.ComponentModel.DataAnnotations;

namespace LanguageLearningApp.API.Application.DTOs
{
    public class LoginDto
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }
}