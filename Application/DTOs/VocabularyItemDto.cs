public class VocabularyItemDto
    {
        public int Id { get; set; }
        public string Word { get; set; }
        public string Translation { get; set; }
        public string Level { get; set; }
        public int TimesEncountered { get; set; }
        public int TimesCorrectlyUsed { get; set; }
        public string LastEncounteredAt { get; set; }
        public bool IsMastered { get; set; }
    }