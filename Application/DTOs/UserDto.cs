namespace LanguageLearningApp.API.Application.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
        public string LanguageLevel { get; set; }
        public int CurrentStreak { get; set; }
        public int TotalPoints { get; set; }
    }
}