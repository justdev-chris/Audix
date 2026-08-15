# Audix

Audix is a desktop media player for audio and video files.

## Features

- Audio playback (MP3, WAV, FLAC, M4A, OGG)
- Video playback (MP4, AVI, MKV, MOV, WEBM)
- Synchronized lyrics (LRC files)
- Embedded lyrics (USLT)
- Album art extraction
- Video frame display
- Playlist management
- Drag & drop support
- Folder scanning
- Dark theme
- Single-file executable (~15MB)

## Installation

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)

### Build from source
```bash
git clone https://github.com/yourusername/Audix
cd Audix
dotnet restore
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

### Run
```bash
./publish/Audix.exe
```

## Usage

1. Launch Audix.exe
2. Click **Open** to select media files (audio or video)
3. Click **Add Folder** to scan entire directories
4. Double-click any track in the playlist to play
5. Use **Play/Pause**, **Stop**, **Next**, **Prev** buttons to control playback
6. Click **Lyrics** button to toggle synchronized lyrics display
7. Click **Art** button to toggle album art/video display
8. Drag & drop files directly into the playlist

## Lyrics Support

Audix loads lyrics in this order:

1. **External LRC file** - Looks for `.lrc` file with the same name as the media file in the same directory
2. **Embedded USLT** - Reads unsynchronized lyrics from MP3 metadata
3. **Embedded lyrics** - Reads lyrics from FLAC and M4A metadata

LRC files should follow the standard format:
```
[00:05.00] First line of lyrics
[00:10.00] Second line of lyrics
[00:15.00] Third line of lyrics
```

The current lyric auto-scrolls into view as the song plays, with the next lines visible below.

## Video Playback

Audix uses LibVLCSharp for video playback, supporting:
- MP4, AVI, MKV, MOV, WEBM formats
- Hardware acceleration where available
- Frame-accurate seeking
- Aspect ratio preservation

Video displays in the main window with album art as fallback for audio-only files.

## Album Art

Audix extracts album art from:
- MP3 (APIC tag)
- FLAC (pictures block)
- M4A (cover art atom)

If no art is found, a placeholder with a music note is displayed.

## Playlist Management

- **Add Files** - Open dialog to select multiple media files
- **Add Folder** - Recursively scan a folder for media files
- **Clear** - Remove all tracks from the playlist
- **Double-click** - Play the selected track
- Playlist persists until cleared or application exits

## Settings

Settings are saved automatically to:
```
%LocalAppData%\Audix\settings.json
```

Settings stored:
- Window size and position
- Lyrics visibility toggle state
- Art visibility toggle state
- Volume level
- Last playlist

## Project Structure

```
Audix/
├── README.md
├── LICENSE
├── Audix.sln
└── src/
    └── Audix/
        ├── Audix.csproj
        ├── Program.cs
        ├── MainForm.cs
        ├── MainForm.Designer.cs
        ├── Models/
        │   ├── Track.cs
        │   ├── LyricLine.cs
        │   └── Settings.cs
        ├── Services/
        │   ├── AudioEngine.cs
        │   ├── PlaylistManager.cs
        │   ├── LyricsService.cs
        │   └── ArtworkService.cs
        └── Utils/
            └── TimeFormatter.cs
```

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| NAudio | 2.2.1 | Audio playback |
| LibVLCSharp | 3.9.0 | Video playback |
| TagLibSharp | 2.3.0 | Metadata, lyrics, album art |
| Newtonsoft.Json | 13.0.3 | Settings |

## Building from Source

1. Install .NET 8 SDK from https://dotnet.microsoft.com/download
2. Clone the repository
3. Run `dotnet restore` to download dependencies
4. Run `dotnet build -c Release` to build
5. Run `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish` to create standalone executable
6. The executable is in `./publish/Audix.exe`

## System Requirements

- Windows 10 or later
- .NET 8 Runtime (included in self-contained build)
- Audio output device

## License

MIT License - see LICENSE.md file for details

---

Built with ❤️ for music lovers
```
