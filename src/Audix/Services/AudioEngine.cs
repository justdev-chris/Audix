using System;
using NAudio.Wave;

namespace Audix.Services
{
    public class AudioEngine : IDisposable
    {
        private WaveOutEvent? outputDevice;
        private AudioFileReader? audioFile;
        private bool disposed;

        public event EventHandler? PlaybackStopped;
        public bool IsPlaying { get; private set; }
        public bool IsPaused { get; private set; }
        public int Duration { get; private set; }
        public int Position { get; private set; }
        public string CurrentFile { get; set; } = string.Empty;

        public bool Load(string filePath)
        {
            try
            {
                Stop();
                audioFile = new AudioFileReader(filePath);
                outputDevice = new WaveOutEvent();
                outputDevice.Init(audioFile);
                outputDevice.PlaybackStopped += (s, e) => PlaybackStopped?.Invoke(this, EventArgs.Empty);
                
                Duration = (int)audioFile.TotalTime.TotalMilliseconds;
                CurrentFile = filePath;
                IsPlaying = false;
                IsPaused = false;
                Position = 0;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Play()
        {
            if (outputDevice == null) return;
            if (IsPaused)
            {
                outputDevice.Resume();
                IsPaused = false;
            }
            else
            {
                outputDevice.Play();
            }
            IsPlaying = true;
        }

        public void Pause()
        {
            if (outputDevice == null || !IsPlaying) return;
            outputDevice.Pause();
            IsPaused = true;
        }

        public void Stop()
        {
            IsPlaying = false;
            IsPaused = false;
            Position = 0;
            outputDevice?.Stop();
        }

        public void Seek(int positionMs)
        {
            if (audioFile == null || Duration == 0) return;
            var pos = Math.Clamp(positionMs, 0, Duration);
            audioFile.CurrentTime = TimeSpan.FromMilliseconds(pos);
            Position = pos;
        }

        public void UpdatePosition()
        {
            if (audioFile == null || !IsPlaying || IsPaused) return;
            Position = (int)audioFile.CurrentTime.TotalMilliseconds;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Stop();
            outputDevice?.Dispose();
            audioFile?.Dispose();
            outputDevice = null;
            audioFile = null;
        }
    }
}
