namespace WordVideoGenerator.Models
{
    public class WordInfo
    {
        public string Word { get; set; }
        public string AudioPath { get; set; }
        public string ImagePath { get; set; }
        public string[] Translations { get; set; }
        public string OutputVideoPath { get; set; }
    }
} 