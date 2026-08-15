using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Audix.Models;
using Audix.Services;
using Audix.Utils;

namespace Audix
{
    public partial class MainForm : Form
    {
        private readonly AudioEngine audio;
        private readonly PlaylistManager playlist;
        private readonly LyricsService lyricsService;
        private readonly ArtworkService artworkService;
        private readonly Timer updateTimer;

        private List<LyricLine> currentLyrics = new List<LyricLine>();
        private int currentLyricIndex = -1;
        private bool showLyrics = true;
        private bool showArt = true;

        private Button playBtn, stopBtn, nextBtn, prevBtn, openBtn, folderBtn, clearBtn, lyricsToggleBtn, artToggleBtn;
        private TrackBar progressBar;
        private Label timeLabel, currentLyricLabel, nextLyricLabel, statusLabel;
        private ListBox playlistBox;
        private PictureBox artBox;

        public MainForm()
        {
            audio = new AudioEngine();
            playlist = new PlaylistManager();
            lyricsService = new LyricsService();
            artworkService = new ArtworkService();

            playlist.TrackChanged += (s, track) => LoadTrack(track);

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
            prevBtn = CreateButton("⏮ Prev", (s, e) => playlist.Previous());
            nextBtn = CreateButton("⏭ Next", (s, e) => playlist.Next());
            lyricsToggleBtn = CreateButton("📝 Lyrics", (s, e) => ToggleLyrics());
            artToggleBtn = CreateButton("🖼️ Art", (s, e) => ToggleArt());

            controlsPanel.Controls.AddRange(new Control[] { playBtn, stopBtn, prevBtn, nextBtn, lyricsToggleBtn, artToggleBtn });
            leftPanel.Controls.Add(controlsPanel);

            var progressPanel = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(26, 26, 46) };
            progressBar = new TrackBar { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100, Value = 0 };
            progressBar.Scroll += (s, e) => audio.Seek((int)(progressBar.Value / 100.0 * audio.Duration));
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
                    if (playlist.Count > 0 && string.IsNullOrEmpty(audio.CurrentFile))
                        playlist.MoveTo(0);
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
                    if (playlist.Count > 0 && string.IsNullOrEmpty(audio.CurrentFile))
                        playlist.MoveTo(0);
                }
            }
        }

        private void AddToPlaylist(string file)
        {
            playlist.Add(file);
            playlistBox.Items.Add(Path.GetFileName(file));
        }

        private void ClearPlaylist()
        {
            Stop();
            playlist.Clear();
            playlistBox.Items.Clear();
        }

        private void PlaySelected()
        {
            if (playlistBox.SelectedIndex >= 0)
                playlist.MoveTo(playlistBox.SelectedIndex);
        }

        private void OnPlaybackStopped(object? sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)(() => OnPlaybackStopped(sender, e)));
                return;
            }

            // Only advance if we actually finished playing (position > 0 means we played something)
            if (audio.Position > 0)
            {
                playlist.Next();
            }
        }

        private void LoadTrack(string file)
        {
            Stop();
            audio.PlaybackStopped -= OnPlaybackStopped;

            System.Threading.Thread.Sleep(50);

            if (!audio.Load(file))
            {
                statusLabel.Text = "Error loading file";
                return;
            }

            audio.PlaybackStopped += OnPlaybackStopped;

            currentLyrics = lyricsService.Extract(file);
            currentLyricIndex = -1;
            currentLyricLabel.Text = currentLyrics.Count > 0 ? currentLyrics[0].Text : "No lyrics";
            nextLyricLabel.Text = "";

            var art = artworkService.Extract(file);
            artBox.Image = art ?? null;

            audio.Play();
            playBtn.Text = "⏸ Pause";
            updateTimer.Start();

            statusLabel.Text = $"Playing: {Path.GetFileName(file)}";
            playlistBox.SelectedIndex = playlist.IndexOf(file);
        }

        private void TogglePlay()
        {
            if (!audio.IsPlaying)
            {
                if (!string.IsNullOrEmpty(audio.CurrentFile))
                {
                    audio.Play();
                    playBtn.Text = "⏸ Pause";
                }
                else if (playlist.Count > 0)
                {
                    playlist.MoveTo(0);
                }
                return;
            }

            if (audio.IsPaused)
            {
                audio.Play();
                playBtn.Text = "⏸ Pause";
            }
            else
            {
                audio.Pause();
                playBtn.Text = "▶ Play";
            }
        }

        private void Stop()
        {
            audio.PlaybackStopped -= OnPlaybackStopped;
            audio.Stop();
            updateTimer.Stop();
            playBtn.Text = "▶ Play";
            progressBar.Value = 0;
            timeLabel.Text = "00:00 / 00:00";
            currentLyricLabel.Text = "Audix";
            nextLyricLabel.Text = "";
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (!audio.IsPlaying || audio.IsPaused) return;

            audio.UpdatePosition();
            var pos = audio.Position;
            var duration = audio.Duration;

            if (duration > 0)
            {
                progressBar.Value = (int)(pos / (double)duration * 100);
                timeLabel.Text = $"{TimeFormatter.Format(pos)} / {TimeFormatter.Format(duration)}";
            }

            UpdateLyrics(pos);
        }

        private void UpdateLyrics(int currentTime)
        {
            if (currentLyrics.Count == 0 || !showLyrics) return;

            var bestIdx = -1;
            for (int i = 0; i < currentLyrics.Count; i++)
            {
                if (currentLyrics[i].TimeMs <= currentTime)
                    bestIdx = i;
                else
                    break;
            }

            if (bestIdx != currentLyricIndex)
            {
                currentLyricIndex = bestIdx;
                if (bestIdx >= 0)
                {
                    currentLyricLabel.Text = currentLyrics[bestIdx].Text;
                    nextLyricLabel.Text = bestIdx + 1 < currentLyrics.Count ? currentLyrics[bestIdx + 1].Text : "";
                }
            }
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
                UpdateLyrics(audio.Position);
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
                    var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Settings>(json);
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
                                playlist.MoveTo(0);
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
                    LastPlaylist = playlist.Tracks.ToList()
                };
                System.IO.File.WriteAllText(settingsPath, Newtonsoft.Json.JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented));
            }
            catch { }
        }
    }
}
