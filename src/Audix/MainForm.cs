using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using NAudio.Wave;
using TagLib;
using Newtonsoft.Json;

namespace Audix
{
    public partial class MainForm : Form
    {
        private WaveOutEvent outputDevice;
        private AudioFileReader audioFile;
        private List<string> playlist = new List<string>();
        private int playlistIndex = -1;
        private List<LyricLine> lyrics = new List<LyricLine>();
        private int currentLyricIndex = -1;
        private Timer updateTimer;
        private string currentFile = "";
        private bool isPlaying = false;
        private bool isPaused = false;
        private int duration = 0;

        private Button playBtn, stopBtn, nextBtn, prevBtn, openBtn, folderBtn, clearBtn, lyricsToggleBtn, artToggleBtn;
        private TrackBar progressBar;
        private Label timeLabel, currentLyricLabel, nextLyricLabel, statusLabel;
        private ListBox playlistBox;
        private PictureBox artBox;
        private bool showLyrics = true;
        private bool showArt = true;

        public MainForm()
        {
            InitializeComponent();
            SetupUI();
            LoadSettings();
            updateTimer = new Timer();
            updateTimer.Interval = 100;
            updateTimer.Tick += UpdateTimer_Tick;
        }

        private void SetupUI()
        {
            this.Text = "Audix";
            this.Size = new Size(1100, 750);
            this.BackColor = Color.FromArgb(10, 10, 26);
            this.MinimumSize = new Size(1000, 700);

            var leftPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(10, 10, 26) };
            leftPanel.Padding = new Padding(5);

            artBox = new PictureBox
            {
                Dock = DockStyle.Top,
                Height = 360,
                BackColor = Color.Black,
                SizeMode = PictureBoxSizeMode.Zoom,
                Padding = new Padding(5)
            };
            leftPanel.Controls.Add(artBox);

            var controlsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = Color.FromArgb(26, 26, 46),
                Padding = new Padding(10),
                FlowDirection = FlowDirection.LeftToRight
            };

            playBtn = CreateButton("▶ Play", (s, e) => TogglePlay());
            stopBtn = CreateButton("⏹ Stop", (s, e) => Stop());
            prevBtn = CreateButton("⏮ Prev", (s, e) => PrevTrack());
            nextBtn = CreateButton("⏭ Next", (s, e) => NextTrack());
            lyricsToggleBtn = CreateButton("📝 Lyrics", (s, e) => ToggleLyrics());
            artToggleBtn = CreateButton("🖼️ Art", (s, e) => ToggleArt());

            controlsPanel.Controls.AddRange(new Control[] { playBtn, stopBtn, prevBtn, nextBtn, lyricsToggleBtn, artToggleBtn });
            leftPanel.Controls.Add(controlsPanel);

            var progressPanel = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(26, 26, 46) };
            progressBar = new TrackBar { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100, Value = 0 };
            progressBar.Scroll += (s, e) => Seek();
            progressPanel.Controls.Add(progressBar);

            timeLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Text = "00:00 / 00:00",
                ForeColor = Color.FromArgb(102, 102, 102),
                BackColor = Color.FromArgb(26, 26, 46),
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 25
            };
            progressPanel.Controls.Add(timeLabel);
            leftPanel.Controls.Add(progressPanel);

            var lyricsPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(10, 10, 26), Padding = new Padding(10) };

            currentLyricLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 100,
                Text = "Audix",
                ForeColor = Color.FromArgb(204, 204, 204),
                BackColor = Color.FromArgb(10, 10, 26),
                Font = new Font("Arial", 22, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            lyricsPanel.Controls.Add(currentLyricLabel);

            nextLyricLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "",
                ForeColor = Color.FromArgb(102, 102, 102),
                BackColor = Color.FromArgb(10, 10, 26),
                Font = new Font("Arial", 18),
                TextAlign = ContentAlignment.MiddleCenter
            };
            lyricsPanel.Controls.Add(nextLyricLabel);
            leftPanel.Controls.Add(lyricsPanel);

            var rightPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(26, 26, 46), Width = 300 };

            var playlistLabel = new Label
            {
                Text = "Playlist",
                ForeColor = Color.FromArgb(0, 255, 136),
                BackColor = Color.FromArgb(26, 26, 46),
                Font = new Font("Arial", 14, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter
            };
            rightPanel.Controls.Add(playlistLabel);

            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.FromArgb(26, 26, 46),
                Padding = new Padding(5)
            };

            openBtn = CreateButton("Open", (s, e) => OpenFiles());
            folderBtn = CreateButton("Folder", (s, e) => AddFolder());
            clearBtn = CreateButton("Clear", (s, e) => ClearPlaylist());
            btnPanel.Controls.AddRange(new Control[] { openBtn, folderBtn, clearBtn });
            rightPanel.Controls.Add(btnPanel);

            playlistBox = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(10, 10, 26),
                ForeColor = Color.FromArgb(204, 204, 204),
                Font = new Font("Arial", 10),
                BorderStyle = BorderStyle.None
            };
            playlistBox.DoubleClick += (s, e) => PlaySelected();
            rightPanel.Controls.Add(playlistBox);

            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(10, 10, 26),
                SplitterDistance = 750,
                Panel1 = { Controls = { leftPanel } },
                Panel2 = { Controls = { rightPanel } }
            };
            this.Controls.Add(splitContainer);

            statusLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Text = "Ready",
                ForeColor = Color.FromArgb(68, 68, 68),
                BackColor = Color.FromArgb(10, 10, 26),
                Height = 25,
                Padding = new Padding(10, 5, 10, 5)
            };
            this.Controls.Add(statusLabel);

            this.FormClosing += (s, e) => SaveSettings();
        }

        private Button CreateButton(string text, EventHandler click)
        {
            var btn = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(42, 42, 78),
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold),
                Width = 85,
                Height = 32
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += click;
            return btn;
        }

        private void OpenFiles()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Multiselect = true;
                dialog.Filter = "Media Files|*.mp3;*.wav;*.flac;*.m4a;*.ogg;*.mp4;*.avi;*.mkv;*.mov;*.webm|All Files|*.*";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (var file in dialog.FileNames)
                        AddToPlaylist(file);
                    if (playlist.Count > 0 && string.IsNullOrEmpty(currentFile))
                        LoadTrack(playlist[0]);
                }
            }
        }

        private void AddFolder()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var extensions = new[] { ".mp3", ".wav", ".flac", ".m4a", ".ogg", ".mp4", ".avi", ".mkv", ".mov", ".webm" };
                    var files = Directory.GetFiles(dialog.SelectedPath, "*.*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        if (extensions.Contains(Path.GetExtension(file).ToLower()))
                            AddToPlaylist(file);
                    }
                    if (playlist.Count > 0 && string.IsNullOrEmpty(currentFile))
                        LoadTrack(playlist[0]);
                }
            }
        }

        private void AddToPlaylist(string file)
        {
            if (!playlist.Contains(file))
            {
                playlist.Add(file);
                playlistBox.Items.Add(Path.GetFileName(file));
            }
        }

        private void ClearPlaylist()
        {
            Stop();
            playlist.Clear();
            playlistBox.Items.Clear();
            currentFile = "";
        }

        private void PlaySelected()
        {
            if (playlistBox.SelectedIndex >= 0)
            {
                playlistIndex = playlistBox.SelectedIndex;
                LoadTrack(playlist[playlistIndex]);
            }
        }

        private void LoadTrack(string file)
        {
            Stop();
            currentFile = file;

            try
            {
                audioFile = new AudioFileReader(file);
                outputDevice = new WaveOutEvent();
                outputDevice.Init(audioFile);
                outputDevice.PlaybackStopped += (s, e) =>
                {
                    if (isPlaying && !isPaused)
                        this.Invoke((MethodInvoker)NextTrack);
                };

                duration = (int)audioFile.TotalTime.TotalMilliseconds;
                isPlaying = true;
                isPaused = false;
                playBtn.Text = "⏸ Pause";
                updateTimer.Start();

                ExtractLyrics(file);
                ExtractArt(file);
                statusLabel.Text = $"Playing: {Path.GetFileName(file)}";
                playlistIndex = playlist.IndexOf(file);
                playlistBox.SelectedIndex = playlistIndex;
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Error: {ex.Message}";
            }
        }

        private void TogglePlay()
        {
            if (!isPlaying)
            {
                if (!string.IsNullOrEmpty(currentFile))
                    LoadTrack(currentFile);
                return;
            }

            isPaused = !isPaused;
            if (isPaused)
            {
                outputDevice.Pause();
                playBtn.Text = "▶ Play";
            }
            else
            {
                outputDevice.Play();
                playBtn.Text = "⏸ Pause";
            }
        }

        private void Stop()
        {
            isPlaying = false;
            isPaused = false;
            updateTimer.Stop();
            outputDevice?.Stop();
            outputDevice?.Dispose();
            outputDevice = null;
            audioFile?.Dispose();
            audioFile = null;
            playBtn.Text = "▶ Play";
            progressBar.Value = 0;
            timeLabel.Text = "00:00 / 00:00";
            currentLyricLabel.Text = "Audix";
            nextLyricLabel.Text = "";
        }

        private void NextTrack()
        {
            if (playlist.Count == 0) return;
            playlistIndex = (playlistIndex + 1) % playlist.Count;
            LoadTrack(playlist[playlistIndex]);
        }

        private void PrevTrack()
        {
            if (playlist.Count == 0) return;
            playlistIndex = (playlistIndex - 1 + playlist.Count) % playlist.Count;
            LoadTrack(playlist[playlistIndex]);
        }

        private void Seek()
        {
            if (audioFile != null && duration > 0)
            {
                var pos = (int)(progressBar.Value / 100.0 * duration);
                audioFile.CurrentTime = TimeSpan.FromMilliseconds(pos);
            }
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (!isPlaying || isPaused || audioFile == null) return;

            var pos = (int)audioFile.CurrentTime.TotalMilliseconds;
            if (duration > 0)
            {
                progressBar.Value = (int)(pos / (double)duration * 100);
                timeLabel.Text = $"{FormatTime(pos)} / {FormatTime(duration)}";
            }

            UpdateLyrics(pos);
        }

        private string FormatTime(int ms)
        {
            var seconds = ms / 1000;
            return $"{seconds / 60:00}:{seconds % 60:00}";
        }

        private void ExtractLyrics(string file)
        {
            lyrics.Clear();
            currentLyricIndex = -1;

            var lrcPath = Path.ChangeExtension(file, ".lrc");
            if (File.Exists(lrcPath))
            {
                try
                {
                    var lines = File.ReadAllLines(lrcPath);
                    foreach (var line in lines)
                    {
                        var match = Regex.Match(line.Trim(), @"^\[(\d+):(\d+\.?\d*)\]\s*(.+)$");
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
                    if (lyrics.Count > 0)
                    {
                        statusLabel.Text += $" | {lyrics.Count} LRC lyrics";
                        return;
                    }
                }
                catch { }
            }

            try
            {
                var tagFile = TagLib.File.Create(file);
                if (!string.IsNullOrEmpty(tagFile.Tag.Lyrics))
                {
                    var lines = tagFile.Tag.Lyrics.Split('\n');
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var text = lines[i].Trim();
                        if (!string.IsNullOrEmpty(text))
                            lyrics.Add(new LyricLine { TimeMs = i * 3000, Text = text });
                    }
                    if (lyrics.Count > 0)
                    {
                        statusLabel.Text += $" | {lyrics.Count} embedded lyrics";
                        return;
                    }
                }
            }
            catch { }

            currentLyricLabel.Text = "No lyrics";
            nextLyricLabel.Text = "";
        }

        private void UpdateLyrics(int currentTime)
        {
            if (lyrics.Count == 0 || !showLyrics) return;

            var bestIdx = -1;
            for (int i = 0; i < lyrics.Count; i++)
            {
                if (lyrics[i].TimeMs <= currentTime)
                    bestIdx = i;
                else
                    break;
            }

            if (bestIdx != currentLyricIndex)
            {
                currentLyricIndex = bestIdx;
                if (bestIdx >= 0)
                {
                    currentLyricLabel.Text = lyrics[bestIdx].Text;
                    nextLyricLabel.Text = bestIdx + 1 < lyrics.Count ? lyrics[bestIdx + 1].Text : "";
                }
            }
        }

        private void ExtractArt(string file)
        {
            try
            {
                var tagFile = TagLib.File.Create(file);
                if (tagFile.Tag.Pictures != null && tagFile.Tag.Pictures.Length > 0)
                {
                    var picture = tagFile.Tag.Pictures[0];
                    using (var ms = new MemoryStream(picture.Data.Data))
                    {
                        artBox.Image = Image.FromStream(ms);
                        return;
                    }
                }
            }
            catch { }
            artBox.Image = null;
        }

        private void ToggleLyrics()
        {
            showLyrics = !showLyrics;
            lyricsToggleBtn.BackColor = showLyrics ? Color.FromArgb(42, 42, 78) : Color.FromArgb(60, 60, 60);
            if (!showLyrics)
            {
                currentLyricLabel.Text = "";
                nextLyricLabel.Text = "";
            }
            else
            {
                UpdateLyrics((int)(audioFile?.CurrentTime.TotalMilliseconds ?? 0));
            }
        }

        private void ToggleArt()
        {
            showArt = !showArt;
            artToggleBtn.BackColor = showArt ? Color.FromArgb(42, 42, 78) : Color.FromArgb(60, 60, 60);
            artBox.Visible = showArt;
        }

        private void LoadSettings()
        {
            try
            {
                var settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Audix", "settings.json");
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    var settings = JsonConvert.DeserializeObject<Settings>(json);
                    if (settings != null)
                    {
                        showLyrics = settings.ShowLyrics;
                        showArt = settings.ShowArt;
                        if (settings.LastPlaylist != null)
                        {
                            foreach (var file in settings.LastPlaylist)
                            {
                                if (File.Exists(file))
                                    AddToPlaylist(file);
                            }
                            if (playlist.Count > 0)
                                LoadTrack(playlist[0]);
                        }
                    }
                }
            }
            catch { }
        }

        private void SaveSettings()
        {
            try
            {
                var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Audix");
                Directory.CreateDirectory(appData);
                var settingsPath = Path.Combine(appData, "settings.json");
                var settings = new Settings
                {
                    ShowLyrics = showLyrics,
                    ShowArt = showArt,
                    LastPlaylist = playlist
                };
                File.WriteAllText(settingsPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
            }
            catch { }
        }
    }

    public class LyricLine
    {
        public int TimeMs { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public class Settings
    {
        public bool ShowLyrics { get; set; } = true;
        public bool ShowArt { get; set; } = true;
        public List<string> LastPlaylist { get; set; } = new List<string>();
    }
}
