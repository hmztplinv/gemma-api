public class UserProfileDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string LanguageLevel { get; set; }
        public string NativeLanguage { get; set; }
        public string LearningLanguage { get; set; }
        public string MemberSince { get; set; }
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
    }