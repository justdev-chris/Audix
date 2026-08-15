using System;
using System.Collections.Generic;
using System.Linq;

namespace Audix.Services
{
    public class PlaylistManager
    {
        private List<string> tracks = new List<string>();
        private int currentIndex = -1;

        public event EventHandler? PlaylistChanged;
        public event EventHandler<string>? TrackChanged;

        public IReadOnlyList<string> Tracks => tracks.AsReadOnly();
        public string? CurrentTrack => currentIndex >= 0 && currentIndex < tracks.Count ? tracks[currentIndex] : null;
        public int Count => tracks.Count;
        public int CurrentIndex => currentIndex;

        public void Add(string filePath)
        {
            if (!tracks.Contains(filePath))
            {
                tracks.Add(filePath);
                PlaylistChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void AddRange(IEnumerable<string> filePaths)
        {
            var added = false;
            foreach (var path in filePaths)
            {
                if (!tracks.Contains(path))
                {
                    tracks.Add(path);
                    added = true;
                }
            }
            if (added) PlaylistChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Remove(int index)
        {
            if (index < 0 || index >= tracks.Count) return;
            tracks.RemoveAt(index);
            if (currentIndex == index) currentIndex = -1;
            else if (currentIndex > index) currentIndex--;
            PlaylistChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Clear()
        {
            tracks.Clear();
            currentIndex = -1;
            PlaylistChanged?.Invoke(this, EventArgs.Empty);
        }

        public void MoveTo(int index)
        {
            if (index < 0 || index >= tracks.Count) return;
            currentIndex = index;
            TrackChanged?.Invoke(this, tracks[currentIndex]);
        }

        public void Next()
        {
            if (tracks.Count == 0) return;
            currentIndex = (currentIndex + 1) % tracks.Count;
            TrackChanged?.Invoke(this, tracks[currentIndex]);
        }

        public void Previous()
        {
            if (tracks.Count == 0) return;
            currentIndex = (currentIndex - 1 + tracks.Count) % tracks.Count;
            TrackChanged?.Invoke(this, tracks[currentIndex]);
        }

        public bool Contains(string filePath) => tracks.Contains(filePath);
        public int IndexOf(string filePath) => tracks.IndexOf(filePath);
        public string? GetTrack(int index) => index >= 0 && index < tracks.Count ? tracks[index] : null;
    }
}
