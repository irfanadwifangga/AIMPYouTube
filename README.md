# AIMP YouTube Support Plugin (C# .NET)

[![Build & Release](https://github.com/irfanadwifangga/AIMPYouTube/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/USER/AIMPYouTube/actions) [![Platform](https://img.shields.io/badge/platform-Windows%20x64-blue.svg)](https://www.aimp.ru) [![Framework](https://img.shields.io/badge/.NET%20Framework-4.8-purple.svg)](https://dotnet.microsoft.com/download/dotnet-framework/net48) [![AIMP](https://img.shields.io/badge/AIMP-v5.00%2B-orange.svg)](https://www.aimp.ru) [![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

A modern plugin for the **[AIMP Media Player](https://www.aimp.ru)** that allows you to play audio directly from **YouTube** (Videos, Shorts, Playlists, Channels, and Handles) within your favorite desktop audio player.

Rewritten from the ground up in **C# (.NET Framework 4.8)** using **[AimpSDK](https://github.com/martin211/aimp_dotnet)** and powered by **[yt-dlp](https://github.com/yt-dlp/yt-dlp)** for fast, reliable stream extraction.

---

## 🌟 Key Features

- **🎧 Wide YouTube URL Support:**
  - Single Videos: `https://www.youtube.com/watch?v=...`, `https://youtu.be/...`, `https://youtube.com/shorts/...`
  - Playlists: `https://www.youtube.com/playlist?list=...`
  - Channels & Handles: `https://youtube.com/@handle`, `https://youtube.com/channel/...`, `https://youtube.com/c/...`, `https://youtube.com/user/...`
- **⚡ Modern Audio Stream Resolution:** Uses `yt-dlp` to extract direct high-quality audio streams with intelligent format selection:
  - **Priority order:** HLS (m3u8) → itag 234/233 → M4A 128kbps (itag 140) → M4A 49kbps (itag 139) → M4A (any) → MP4 → fallback
  - Automatically avoids WebM/Opus formats that are incompatible with AIMP's BASS decoder engine.
  - Cached stream URLs are auto-invalidated when format incompatibility is detected.
- **🖼️ Album Art & Thumbnails:** Displays original YouTube high-resolution thumbnails directly in AIMP's player and playlist.
- **⏱️ Metadata & Duration:** Automatically parses track duration, title, and channel information via YouTube Data API v3 and local caching.
- **🌐 Multilingual Support:** Includes 20 built-in languages (Bahasa Indonesia, English, Russian, Spanish, German, French, Japanese, and more).
- **⚙️ Integrated Preferences:** Configurable from AIMP's Options (`Ctrl + P`) > Plugins > YouTube Support.
- **🚫 Track Exclusions:** Easily exclude unwanted tracks from playlists with right-click > _Add to Exclusions_.

---

## 📦 Installation

### Method 1: Pre-built Release (Recommended)

1. Download the latest `AIMPYouTube-vX.X.X.zip` from **[GitHub Releases](../../releases)**.
2. Extract the `AIMPYouTube` folder from the archive.
3. Move the `AIMPYouTube` folder into your AIMP Plugins directory:
   - **Standard:** `C:\Program Files\AIMP\Plugins\AIMPYouTube` (or `%APPDATA%\AIMP\Plugins\AIMPYouTube`)
4. Ensure `yt-dlp.exe` is available (placed in the plugin folder, installed via WinGet/Chocolatey, or downloaded via plugin preferences).
5. Start or restart **AIMP**.

### Method 2: Build from Source

#### Prerequisites

- Windows 10 / 11 (x64)
- [.NET SDK 8.0+ or .NET Framework 4.8 Developer Pack](https://dotnet.microsoft.com/download)
- [AIMP Media Player v5.0+](https://www.aimp.ru)

#### Build & Deploy Command

```powershell
# Clone the repository
git clone https://github.com/USER/AIMPYouTube.git
cd AIMPYouTube

# Build and deploy automatically to AIMP
powershell -ExecutionPolicy Bypass -File "deploy-and-test.ps1"
```

---

## 🚀 How to Use

1. **Add YouTube Track / Playlist:**
   - In AIMP, click the **`+`** (Add) button below the playlist.
   - Select **`YouTube URL...`** (or localized menu item in your language).
   - Paste any YouTube video, playlist, or channel URL.
   - Check _"Create new playlist for this item"_ if you want a dedicated playlist tab.
   - Click **`Add to AIMP`**.

2. **Context Menu Actions:**
   - Right-click any YouTube track in the playlist:
     - **YouTube: Open in Browser:** Opens the original YouTube link in your default web browser.
     - **YouTube: Add to Exclusions:** Removes the track and prevents it from being added again.

3. **Plugin Settings:**
   - Press `Ctrl + P` to open AIMP Preferences.
   - Navigate to **Plugins** > **YouTube Support**.
   - Configure custom API keys, `yt-dlp` executable paths, and audio format arguments.

---

## ⚙️ Configuration Reference

### yt-dlp Settings (Preferences > Plugins > YouTube Support)

| Setting | Default | Description |
| --- | --- | --- |
| **yt-dlp Path** | _(auto-detect)_ | Path to `yt-dlp.exe`. Auto-detected from plugin folder, config folder, or PATH. Auto-downloaded if missing. |
| **Format Parameters** | `-f ba[protocol^=m3u8]/234/233/140/139/ba[ext=m4a]/best[ext=mp4]/best` | yt-dlp format selection string. See below for details. |
| **Cookies Browser** | `none` | Browser to extract cookies from (e.g., `chrome`, `firefox`, `edge`). Required for age-restricted or private videos. |
| **Timeout** | `30` seconds | Maximum time to wait for yt-dlp to resolve a stream URL. |
| **Force yt-dlp** | `true` | Always use yt-dlp instead of attempting direct YouTube API extraction first. |

### Audio Format Priority

The default format string ensures maximum compatibility with AIMP's BASS audio engine:

```
-f ba[protocol^=m3u8]/234/233/140/139/ba[ext=m4a]/best[ext=mp4]/best
```

| Priority | Format | Codec | Bitrate | Notes |
| --- | --- | --- | --- | --- |
| 1st | `ba[protocol^=m3u8]` | HLS manifest | Adaptive | Most reliable for AIMP streaming |
| 2nd | `234` | HLS (high) | Adaptive | YouTube HLS audio-only, high quality |
| 3rd | `233` | HLS (low) | Adaptive | YouTube HLS audio-only, lower quality |
| 4th | `140` | AAC (M4A) | 128kbps | Best direct audio quality for AIMP |
| 5th | `139` | AAC (M4A) | 49kbps | Low quality fallback |
| 6th | `ba[ext=m4a]` | Any M4A | Varies | Any available M4A stream |
| 7th | `best[ext=mp4]` | MP4 | Varies | Video+audio MP4 container |
| 8th | `best` | Any | Varies | Last resort fallback |

> ⚠️ **Important:** WebM/Opus formats (itag 249/250/251) are **not compatible** with AIMP's BASS decoder and will cause `Code 41: Unsupported file format` errors. The plugin automatically avoids these formats.

---

## 🏗️ Architecture

Built as a pure **C# .NET Framework 4.8** plugin using the [AIMP .NET SDK](https://github.com/martin211/aimp_dotnet):

```
┌──────────────────────────────────────────────────────┐
│                    AIMP Player                       │
│                  (Native Host)                       │
├──────────────────────────────────────────────────────┤
│  AIMPYouTube.dll  (.NET 4.8 Plugin)                  │
│                                                      │
│  ┌─────────────┐  ┌──────────────┐  ┌─────────────┐ │
│  │ PlayerHook  │  │  AlbumArt    │  │  AddUrlForm  │ │
│  │ intercepts  │  │  thumbnail   │  │  YouTube URL │ │
│  │ youtube://  │  │  provider    │  │  input UI    │ │
│  │ URLs        │  │              │  │              │ │
│  └──────┬──────┘  └──────────────┘  └──────────────┘ │
│         │                                            │
│  ┌──────▼──────┐  ┌──────────────┐  ┌─────────────┐ │
│  │ YtDlpService│  │  YouTubeAPI  │  │   Config     │ │
│  │ stream URL  │  │  metadata &  │  │   settings   │ │
│  │ extraction  │  │  playlists   │  │   & cache    │ │
│  └──────┬──────┘  └──────────────┘  └─────────────┘ │
├─────────┼────────────────────────────────────────────┤
│  yt-dlp.exe  │  External tool for stream extraction  │
└──────────────┴───────────────────────────────────────┘
```

### Playback Flow

1. User adds a YouTube URL → Plugin creates playlist entry with `youtube://<videoId>/Title.mp4`
2. When AIMP plays the track, `YouTubePlayerHook.OnCheckURL()` intercepts the URL
3. `YtDlpService.GetStreamUrl()` calls `yt-dlp` to resolve a direct audio stream URL
4. URL is enriched with file extension hints (e.g., `&file=audio.m4a`) for BASS decoder
5. AIMP's HTTP client downloads and plays the stream

---

## 📁 Project Structure

```
AIMPYouTube/
├── Configuration/              # Settings & data models
│   ├── Config.cs               # All plugin settings, save/load, migration
│   ├── TrackInfo.cs            # YouTube track metadata model
│   ├── MonitorUrl.cs           # Playlist monitoring configuration
│   └── PlaylistConfig.cs       # User playlist configuration
├── Core/                       # Utilities & localization
│   ├── Tools.cs                # URL parsing, string helpers
│   └── Localization.cs         # Multi-language support
├── Extensions/                 # AIMP SDK integration hooks
│   ├── YouTubePlayerHook.cs    # URL interceptor (youtube:// → stream URL)
│   ├── AlbumArtProvider.cs     # Thumbnail/artwork loader
│   └── PlaylistListener.cs     # Playlist change monitoring
├── Services/                   # Backend services
│   ├── YtDlpService.cs         # yt-dlp process execution & stream extraction
│   ├── YouTubeDataService.cs   # YouTube Data API v3 client
│   └── OAuthService.cs         # Google OAuth 2.0 authentication
├── UI/                         # User interface
│   ├── AddUrlForm.cs           # "Add YouTube URL" dialog
│   └── OptionsPanel.cs         # Plugin preferences panel
├── Langs/                      # 20 localization files (.lng)
├── Plugin.cs                   # Main entry point ([AimpPlugin])
├── AIMPYouTube.csproj          # .NET Framework 4.8 project file
├── AIMPYouTube.sln             # Solution file
├── deploy-and-test.ps1         # Build, deploy & launch automation
├── check-requirements.ps1      # Environment verification script
├── .gitignore
├── LICENSE
└── README.md
```
---

## 🔧 Troubleshooting

| Problem | Cause | Solution |
| --- | --- | --- |
| **Code 41: Unsupported file format** | AIMP received a WebM/Opus stream (`Content-Type: audio/webm`) that BASS cannot decode | Update the plugin to the latest version. The format string should be `-f ba[protocol^=m3u8]/234/233/140/139/ba[ext=m4a]/best[ext=mp4]/best`. Delete `Cache.json` in `%APPDATA%\AIMP\AIMPYouTube\` to clear stale URLs. |
| **Low audio quality** | yt-dlp selected a low-bitrate format (e.g., itag 139 at 49kbps) | Check if HLS (m3u8) or itag 140 (128kbps M4A) is available for the video. Some videos may only offer low-quality audio. |
| **"yt-dlp.exe not found"** | yt-dlp is not installed or not in plugin directory | The plugin will attempt to auto-download yt-dlp. You can also install it via `winget install yt-dlp` or place `yt-dlp.exe` in the plugin folder. |
| **Timeout / no stream URL** | yt-dlp takes too long or YouTube blocks the request | Increase the timeout value in settings. Try setting a cookies browser (e.g., `chrome`) for authentication. Run `yt-dlp -U` to update to the latest version. |
| **Age-restricted videos** | YouTube requires login to access | Set **Cookies Browser** in settings to `chrome`, `firefox`, or `edge` (must be logged into YouTube in that browser). |
| **Plugin not appearing in AIMP** | DLL not loaded or wrong directory | Ensure the plugin is in `%APPDATA%\AIMP\Plugins\AIMPYouTube\` or `C:\Program Files\AIMP\Plugins\AIMPYouTube\`. Restart AIMP. |

---

## 🤝 Acknowledgements

- Original C++ AIMPYouTube plugin by **[Eddy](https://github.com/Eddy87)**.
- .NET AIMP SDK Bridge by **[Martin211 / Evgeniy Bogdan](https://github.com/martin211/aimp_dotnet)**.
- Stream resolution powered by **[yt-dlp](https://github.com/yt-dlp/yt-dlp)**.
- Music player software by **[Artem Izmaylov (AIMP Development Team)](https://www.aimp.ru)**.

---

## 📄 License

This project is open source and available under the [MIT License](LICENSE).
