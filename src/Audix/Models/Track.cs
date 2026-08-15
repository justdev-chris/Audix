namespace Audix.Models
{
    public class Track
    {
        public string FilePath { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public int Duration { get; set; }
    }
}
