using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Audix.Models;

namespace Audix.Services
{
    public class LyricsService
    {
        public List<LyricLine> Extract(string filePath)
        {
            var lyrics = new List<LyricLine>();

            // Try LRC file
            var lrcPath = Path.ChangeExtension(filePath, ".lrc");
            if (File.Exists(lrcPath))
            {
                try
                {
                    var lines = File.ReadAllLines(lrcPath);
                    var regex = new Regex(@"^\[(\d+):(\d+\.?\d*)\]\s*(.+)$");
                    foreach (var line in lines)
                    {
                        var match = regex.Match(line.Trim());
                        if (match.Success)
                        {
                            var minutes = int.Parse(match.Groups[1].Value);
                            var seconds = double.Parse(match.Groups[2].Value);
                            var timeMs = (int)((minutes * 60 + seconds) * 1000);
                            var text = match.Groups[3].Value.Trim();
                            if (!string.IsNullOrEmpty(text))
                                lyrics.Add(new LyricLine { TimeMs = timeMs, Text = text });
                        }
                    }
                    lyrics = lyrics.OrderBy(l => l.TimeMs).ToList();
                    if (lyrics.Count > 0) return lyrics;
                }
                catch { }
            }

            // Try embedded lyrics
            try
            {
                var tagFile = TagLib.File.Create(filePath);
                if (!string.IsNullOrEmpty(tagFile.Tag.Lyrics))
                {
                    var lines = tagFile.Tag.Lyrics.Split('\n');
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var text = lines[i].Trim();
                        if (!string.IsNullOrEmpty(text))
                            lyrics.Add(new LyricLine { TimeMs = i * 3000, Text = text });
                    }
                    if (lyrics.Count > 0) return lyrics;
                }
            }
            catch { }

            return lyrics;
        }
    }
}
