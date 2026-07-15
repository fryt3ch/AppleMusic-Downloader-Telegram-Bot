
<h1 align="center">
  AppleMusic Downloader Telegram Bot
</h1>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet" alt=".NET 10.0" />
  <img src="https://img.shields.io/badge/Python-3.12-3776AB?style=flat-square&logo=python" alt="Python 3.12" />
  <img src="https://img.shields.io/badge/Docker-✓-2496ED?style=flat-square&logo=docker" alt="Docker" />
  <img src="https://img.shields.io/badge/SQLite-✓-003B57?style=flat-square&logo=sqlite" alt="SQLite" />
  <img src="https://img.shields.io/badge/License-MIT-yellow?style=flat-square" alt="License: MIT" />
</p>

<p align="center">
  A Telegram bot for searching and downloading music from Apple Music.<br/>
  Supports inline search, track/album/playlist downloads, and smart caching.
</p>

---

## Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Requirements](#requirements)
- [Setup](#setup)
  - [1. Clone & Configure](#1-clone--configure)
  - [2. Environment Variables](#2-environment-variables)
  - [3. Get Apple Music JWT Token](#3-get-apple-music-jwt-token)
  - [4. Export Apple Music Cookies](#4-export-apple-music-cookies)
  - [5. Docker Compose Profiles](#5-docker-compose-profiles)
- [Usage](#usage)
  - [Inline Search](#inline-search)
  - [Search Modes](#search-modes)
  - [URL Input](#url-input)
  - [Inline Keyboard Navigation](#inline-keyboard-navigation)
  - [Commands](#commands)
- [Project Structure](#project-structure)

---

## Features

- **Inline Search** — search Apple Music directly from any Telegram chat via `@YourBot query`
- **Flexible Search Modes** — search by song, album, artist, or playlist; browse artist discography, album tracklists, and playlist contents
- **URL Parsing** — paste an Apple Music link (`music.apple.com/...`) directly into the chat
- **Song Download** — download individual tracks as `.m4a` files with metadata
- **Bulk Download** — download entire albums or playlists with live progress updates
- **Smart Caching** — downloaded tracks are cached in a Telegram storage chat and served instantly on repeat requests
- **Webhook & Polling** — supports both webhook mode (for production) and long-polling (for development)
- **Local Bot API** — optional profile for running a local Telegram Bot API server to bypass file size limits
- **Rate Limiting** — one concurrent download per user prevents overload
- **Auto-Migration** — EF Core migrations run automatically on startup

## Architecture

```
┌──────────┐     Telegram Bot API      ┌───────────────────────────────┐
│  User    │ ◄──────────────────────► │  ASP.NET Core Web App (.NET)  │
│ (Telegram)│                          │                               │
└──────────┘                          │  ┌─────────────────────────┐  │
                                      │  │     UpdateHandler       │  │
                                      │  │  (message/inline/cb)    │  │
                                      │  └───────┬───────┬─────────┘  │
                                      │          │       │            │
                                      │          ▼       ▼            │
                                      │  ┌──────────┐ ┌───────────┐  │
                                      │  │MusicSvc  │ │SongCacher │  │
                                      │  │(catalog) │ │(SQLite)   │  │
                                      │  └────┬─────┘ └─────┬─────┘  │
                                      │       │             │         │
                                      └───────┼─────────────┼─────────┘
                                              │             │
                              Apple Music     │   cache     │  Telegram
                              Catalog API     │   miss      │  Storage Chat
                             (JWT auth)       │             │  (file_id cache)
                                              │             │
                                      ┌───────▼─────────────▼─────────┐
                                      │   GamdlSongFileProvider       │
                                      │   spawns Python subprocess     │
                                      └───────────────┬───────────────┘
                                                      │
                                              ┌───────▼────────┐
                                              │ gamdl-bridge.py│
                                              │  (gamdl 3.7.4) │
                                              │  cookies auth   │
                                              └───────┬────────┘
                                                      │
                                              ┌───────▼────────┐
                                              │   .m4a file    │
                                              └────────────────┘
```

1. User sends a query or URL → Telegram delivers it to the bot.
2. **MusicService** resolves metadata via Apple Music Catalog API (JWT developer token).
3. **SongCacher** checks SQLite + Telegram storage chat for a cached copy.
4. On cache miss, **GamdlSongFileProvider** spawns a Python subprocess running `gamdl-bridge.py`.
5. `gamdl-bridge.py` uses **gamdl** (cookies authentication) to download and decrypt the `.m4a` file from Apple Music servers.
6. The file is uploaded to the storage chat, cached, and sent to the user.

## Tech Stack

| Layer            | Technology                              |
|------------------|-----------------------------------------|
| Backend          | ASP.NET Core (.NET 10.0)                |
| Telegram API     | Telegram.Bot (custom fork)              |
| Database         | SQLite via EF Core                      |
| Download Engine  | Python 3 + gamdl (3.7.4)                |
| Containerization | Docker, Docker Compose                  |
| Resilience       | Polly (retry policies), KeyedSemaphores |
| Validation       | FluentValidation                        |

## Requirements

> **All three** are required for full functionality.

1. **Apple Music Subscription** — needed to export authentication cookies for track decryption. Without this, downloads will fail.
2. **Apple Developer JWT Token** — for Apple Music Catalog API access (search + metadata). See [Get Apple Music JWT Token](#3-get-apple-music-jwt-token).
3. **Telegram Bot Token** — create a bot via [@BotFather](https://t.me/BotFather).

## Setup

### 1. Clone & Configure

```bash
git clone https://github.com/frytech/AppleMusic-Downloader-Telegram-Bot.git
cd AppleMusic-Downloader-Telegram-Bot
cp .env.example .env   # or create .env manually
```

### 2. Environment Variables

Fill in the `.env` file with your credentials:

| Variable                                 | Required | Default                            | Description                                    |
|------------------------------------------|:--------:|------------------------------------|------------------------------------------------|
| `TELEGRAM__BOT_TOKEN`                    |    ✅    | —                                  | Telegram bot token from @BotFather             |
| `TELEGRAM__STORAGE_CHAT_ID`              |    ✅    | —                                  | Telegram chat/group ID for caching files       |
| `APPLE_MUSIC__API_TOKEN`                 |    ✅    | —                                  | Apple Music JWT developer token                |
| `APPLE_MUSIC__DEFAULT_STORE`             |          | `us`                               | Default iTunes storefront (`us`, `kz`, `ru`, …)|
| `APPLE_MUSIC_DOWNLOADER__COOKIES_PATH`   |          | `/app/data/cookies.txt`            | Path to Netscape-format cookies file           |
| `TELEGRAM__SERVER_API_URL`               |          | `https://api.telegram.org`         | Telegram API endpoint                          |
| `TELEGRAM__WEBHOOK__ENABLED`             |          | `false`                            | Enable webhook mode                            |
| `TELEGRAM__WEBHOOK__URL`                 |          | —                                  | Webhook callback URL (required if enabled)     |
| `TELEGRAM__WEBHOOK__MAX_CONNECTIONS`     |          | `50`                               | Max simultaneous webhook connections           |
| `TELEGRAM_API_ID`                        |          | —                                  | For local bot API profile only                 |
| `TELEGRAM_API_HASH`                      |          | —                                  | For local bot API profile only                 |
| `LOGGING__LOGLEVEL__DEFAULT`             |          | `Debug`                            | Log level (`Trace`, `Debug`, `Info`, …)        |

### 3. Get Apple Music JWT Token

The JWT developer token can be obtained without an Apple Developer account:

1. Log in to [music.apple.com](https://music.apple.com) in your browser.
2. Open DevTools (F12) → **Console** tab.
3. Run the following command:

```js
MusicKit.getInstance().developerToken
```

4. Copy the returned token string and set it as `APPLE_MUSIC__API_TOKEN` in your `.env` file.

> Tokens expire after a few hours. You will need to repeat this step periodically, or set up automated token refresh.

### 4. Export Apple Music Cookies

The bot uses gamdl which requires Netscape-format cookies from an active Apple Music session:

1. Install a browser extension like [cookies.txt](https://github.com/kairi003/Get-cookies.txt-LOCALLY) (Chrome/Firefox).
2. Log in to [music.apple.com](https://music.apple.com).
3. Export cookies in **Netscape format** and save as `./data/bot/cookies.txt`.
4. Ensure the account has an **active Apple Music subscription**.

> The .NET backend validates the subscription via gamdl on each download attempt.

### 5. Docker Compose Profiles

```bash
# Standard mode — uses public Telegram API
docker compose up -d

# Local API mode — runs a local telegram-bot-api server
docker compose --profile local-api up -d
```

| Profile     | Containers               | Use Case                                    |
|-------------|--------------------------|---------------------------------------------|
| *(default)* | Bot only                 | Standard deployment with public Telegram API |
| `local-api` | Bot + local Telegram API | Lower latency, bypasses file size limits     |

Local API server is accessible at `http://10.77.77.1:8081` and requires `TELEGRAM_API_ID` / `TELEGRAM_API_HASH` — [get them here](https://my.telegram.org/apps).

The bot will auto-run EF Core migrations on first start, creating the SQLite database at `./data/bot/app.db`.

## Usage

### Inline Search

Type `@YourBotName` followed by a query in any Telegram chat:

```
@YourBotName Imagine Dragons
@YourBotName song:Bohemian Rhapsody
@YourBotName album:Dark Side of the Moon
@YourBotName artist:The Weeknd
```

Select a result to receive the track(s).

### Search Modes

| Prefix            | Mode                | Description                                |
|-------------------|---------------------|--------------------------------------------|
| *(no prefix)*     | Search All          | Songs + albums + artists + playlists       |
| `all:`            | Search All          | Same as no prefix, explicit form           |
| `song:`           | Search Songs        | Songs only                                 |
| `album:`          | Search Albums       | Albums only                                |
| `artist:`         | Search Artists      | Artists only                               |
| `playlist:`       | Search Playlists    | Playlists only                             |
| `album-songs:`    | List Album Tracks   | All songs in an album (by ID or URL)       |
| `playlist-songs:` | List Playlist Tracks| All tracks in a playlist (by ID or URL)    |
| `artist-songs:`   | Artist's Songs      | All songs by an artist (by ID or URL)      |
| `artist-albums:`  | Artist's Albums     | All albums by an artist (by ID or URL)     |
| `artist-playlists:`| Artist's Playlists  | Playlists featuring an artist (by ID/URL)  |

### URL Input

Paste an Apple Music link directly into the chat:

```
https://music.apple.com/us/album/after-hours/1499378108
https://music.apple.com/us/song/blinding-lights/1499378115
```

The bot automatically detects the resource type (song, album, artist, or playlist) and responds accordingly.

### Inline Keyboard Navigation

After searching, use inline buttons to:
- **Download** a single song
- **Download all** tracks from an album/playlist
- **Browse** an artist's albums, songs, or playlists
- View an album's full tracklist

During bulk downloads, a live status message shows progress: `Songs gathered: 5 of 12`.

### Commands

| Command  | Description                        |
|----------|------------------------------------|
| `/start` | Welcome message with quick actions |

## Project Structure

```
.
├── docker-compose.yml              # Main compose file
├── docker-compose.override.yml     # Local API + networking overrides
├── Dockerfile                      # Multi-stage: .NET SDK → ASP.NET + Python
├── .env                            # Environment variables (sensitive)
├── src/
│   ├── python/
│   │   └── gamdl-bridge.py         # Python download bridge (gamdl)
│   └── frytech.AppleMusicTools.Downloader.TelegramBot/
│       ├── Program.cs              # App entry point, DI wiring
│       ├── Configuration/
│       │   └── AppSettings.cs      # Typed config with validation
│       ├── Extensions/             # Middleware, mappings
│       ├── Models/                 # Entities, enums, records
│       │   └── Database/           # EF Core entities (CachedSong, User)
│       ├── Migrations/             # EF Core SQLite migrations
│       └── Services/
│           ├── UpdateHandler.cs    # Main handler: messages, inline, callbacks
│           ├── MusicService.cs     # Apple Music catalog API client
│           ├── GamdlSongFileProvider.cs  # Python bridge invocation
│           ├── SongCacher.cs       # SQLite + Telegram storage cache
│           ├── SongSender.cs       # Sends audio to users
│           └── ...                 # Interfaces, helpers
└── data/
    ├── bot/
    │   ├── app.db                  # SQLite database (auto-created)
    │   └── cookies.txt             # Apple Music cookies (user-provided)
    └── telegram/                   # Local Bot API persistence
```

---

<p align="center">
  <sub>Built with .NET, Python, and Docker</sub>
</p>
