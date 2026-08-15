using System.Collections.Generic;

namespace Audix.Models
{
    public class Settings
    {
        public bool ShowLyrics { get; set; } = true;
        public bool ShowArt { get; set; } = true;
        public int Volume { get; set; } = 80;
        public List<string> LastPlaylist { get; set; } = new List<string>();
    }
}
