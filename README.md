<h1 align="center">Apple Music Downloader Telegram Bot</h1>

<p align="center">
  Search Apple Music, download tracks, albums, and playlists from Telegram.<br/>
  Metadata always comes from Apple Music; audio can come from Apple Music or YouTube.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet" alt=".NET 10.0" />
  <img src="https://img.shields.io/badge/Python-3.12-3776AB?style=flat-square&logo=python" alt="Python 3.12" />
  <img src="https://img.shields.io/badge/Docker-2496ED?style=flat-square&logo=docker&logoColor=white" alt="Docker" />
  <img src="https://img.shields.io/badge/SQLite-003B57?style=flat-square&logo=sqlite&logoColor=white" alt="SQLite" />
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-yellow?style=flat-square" alt="License: MIT" /></a>
</p>

## Contents

- [Features](#features)
- [Tech Stack](#tech-stack)
- [Quick Start](#quick-start)
- [Requirements](#requirements)
- [Setup](#setup)
- [Configuration](#configuration)
- [How It Works](#how-it-works)
- [Usage](#usage)
- [Troubleshooting](#troubleshooting)
- [Project Layout](#project-layout)
- [License](#license)

## Features

- Inline search in any Telegram chat.
- Search songs, albums, artists, and playlists, or paste an Apple Music URL.
- Download single tracks and full albums/playlists with metadata.
- Queue with positions, cancellation, and a per-request progress message.
- Parallel downloads with a global provider limit and a configurable cooldown.
- Cache in SQLite plus a Telegram storage chat, so repeat requests are instant.
- Pluggable audio providers: Apple Music (`gamdl`) or YouTube (`yt-dlp`).
- Webhook and long-polling modes.
- Optional local Telegram Bot API server for larger files.
- EF Core migrations run automatically on startup.

> The bot is designed to run as a **single instance**. Cache-miss downloads are protected by an in-process keyed lock. Horizontal scaling would require a distributed lock or database claim.

## Tech Stack

| Layer | Technology |
|---|---|
| Application | ASP.NET Core on .NET 10 |
| Telegram | Telegram.Bot (custom fork) |
| Catalog | Apple Music Catalog API (JWT) |
| Database | SQLite via EF Core |
| Audio providers | Python 3.12, gamdl 3.7.4, yt-dlp + Deno |
| Infrastructure | Docker Compose |
| Validation | FluentValidation |

## Quick Start

```bash
git clone https://github.com/frytech/AppleMusic-Downloader-Telegram-Bot.git
cd AppleMusic-Downloader-Telegram-Bot

cp .env.example .env
# Fill in the required values (see below)

docker compose up -d --build
docker compose logs -f applemusic-bot
```

Minimal `.env` (YouTube mode):

```dotenv
TELEGRAM__BOT_TOKEN=123456:ABC...
TELEGRAM__STORAGE_CHAT_ID=-100...
APPLE_MUSIC__API_TOKEN=eyJ...

DOWNLOADER__PROVIDER=ytdlp
DOWNLOADER__YTDLP__COOKIES_PATH=/app/data/youtube_cookies.txt
```

For Apple Music audio, set `DOWNLOADER__PROVIDER=gamdl` and point `DOWNLOADER__GAMDL__COOKIES_PATH` at your Apple cookies. The JWT token is required in both modes; an Apple Music subscription is required only for `gamdl`.

## Requirements

| Requirement | Needed for | Notes |
|---|---|---|
| Telegram bot token | everything | Create one via [@BotFather](https://t.me/BotFather). |
| Telegram storage chat | everything | Private group or channel where the bot can upload cached audio. Add the bot as an admin and use the numeric chat ID (`-100...`). |
| Apple Music JWT token | everything | Used for search, metadata, and cache IDs. |
| Apple Music subscription + Apple cookies | `gamdl` | Required to decrypt Apple Music `.m4a` files. |
| YouTube cookies | `ytdlp` | Optional but strongly recommended. Use a dedicated account. |
| Docker + Docker Compose | everything | `ffmpeg`, Python, and Deno are included in the image. |

## Setup

### 1. Telegram

1. Create a bot via [@BotFather](https://t.me/BotFather) and copy its token into `TELEGRAM__BOT_TOKEN`.
2. Create a private group or channel for cached files.
3. Add the bot as an administrator so it can upload audio.
4. Put its numeric chat ID into `TELEGRAM__STORAGE_CHAT_ID`.

### 2. Apple Music JWT

1. Sign in at [music.apple.com](https://music.apple.com).
2. Open DevTools (`F12`) and switch to the **Console** tab.
3. Run:

```js
MusicKit.getInstance().developerToken
```

4. Copy the token into `APPLE_MUSIC__API_TOKEN`.

The token expires periodically. Refresh it when searches start failing with `401` responses.

### 3. Cookies

Keep Apple and YouTube cookies in separate files. The provided `.env.example` uses:

```text
data/bot/apple_cookies.txt
data/bot/youtube_cookies.txt
```

Both map to `/app/data/...` inside the container. If you prefer a single file, the compose defaults use `/app/data/cookies.txt`, but separate files avoid accidental overwrites when switching providers.

Keep `.env` and cookie files out of version control. Both are excluded by the repository's `.gitignore`.

#### Apple Music cookies (`gamdl`)

Export Netscape cookies from a signed-in [music.apple.com](https://music.apple.com) session (for example with [cookies.txt](https://github.com/kairi003/Get-cookies.txt-LOCALLY)) and save them as `data/bot/apple_cookies.txt`. The account must have an active Apple Music subscription.

#### YouTube cookies (`ytdlp`)

YouTube cookies are optional but improve reliability. Use a dedicated Google account that you do not use in a browser afterwards, and export Netscape cookies from the same egress IP as the bot to `data/bot/youtube_cookies.txt`.

The bridge gives each yt-dlp process a temporary copy of the jar and merges updated cookies back under a lock, so rotation is preserved during parallel downloads. If the session expires, replace the file with a fresh export.

### 4. Run

```bash
docker compose up -d --build
```

The SQLite database is created automatically at `./data/bot/app.db`.

### 5. Local Bot API (optional)

The local Bot API server lifts the public Bot API file-size limit.

1. Get `api_id` and `api_hash` from [my.telegram.org](https://my.telegram.org/apps).
2. Add to `.env`:

```dotenv
TELEGRAM_API_ID=123456
TELEGRAM_API_HASH=...
TELEGRAM__SERVER_API_URL=http://telegram-bot-api:8081
```

3. Start with the profile:

```bash
docker compose --profile local-api up -d --build
```

### 6. Webhook mode (optional)

By default the bot uses long polling. To use webhooks:

```dotenv
TELEGRAM__WEBHOOK__ENABLED=true
TELEGRAM__WEBHOOK__URL=https://example.com
TELEGRAM__WEBHOOK__PATH=/api/bot/update
TELEGRAM__WEBHOOK__SECRET_TOKEN=change-me
```

The URL must be publicly reachable and routed to the bot on port `8080`.

## Configuration

### Core

| Variable | Required | Default | Description |
|---|:---:|---|---|
| `TELEGRAM__BOT_TOKEN` | Yes | — | Bot token from BotFather. |
| `TELEGRAM__STORAGE_CHAT_ID` | Yes | — | Chat ID used to cache uploaded audio. |
| `APPLE_MUSIC__API_TOKEN` | Yes | — | Apple Music JWT developer token. |
| `APPLE_MUSIC__DEFAULT_STORE` | No | `us` | Storefront used for search and metadata (`us`, `kz`, `ru`, ...). |
| `DOWNLOADER__PROVIDER` | No | `gamdl` | `gamdl` for Apple Music or `ytdlp` for YouTube. |
| `DOWNLOADER__GAMDL__COOKIES_PATH` | `gamdl` | `/app/data/cookies.txt` | Apple Music cookies file. |
| `DOWNLOADER__YTDLP__COOKIES_PATH` | No | `/app/data/cookies.txt` | Optional YouTube cookies file. |
| `DOWNLOADER__PROCESS_TIMEOUT_SECONDS` | No | `300` | Timeout for a provider subprocess. |

### Downloads and queue

| Variable | Default | Description |
|---|---:|---|
| `DOWNLOADER__MAX_CONCURRENCY` | `3` | Maximum simultaneous provider calls, and maximum songs downloaded in parallel within one request. |
| `DOWNLOADER__DELAY_AFTER_DOWNLOAD_SECONDS` | `3` | Cooldown after each provider attempt, including failures. |
| `TELEGRAM__DOWNLOADS__MAX_CONCURRENCY` | `2` | Number of download requests processed in parallel. |
| `TELEGRAM__DOWNLOADS__MAX_PENDING_SONGS` | `2000` | Soft cap on total songs waiting in the queue. |
| `TELEGRAM__DOWNLOADS__MAX_PENDING_BULK_PER_USER` | `1` | Maximum pending album/playlist requests per user. |
| `TELEGRAM__DOWNLOADS__MAX_SONGS_PER_BATCH` | `250` | Maximum songs queued by one album/playlist batch request. Must be between 1 and 250. |
| `TELEGRAM__DOWNLOADS__SONGS_PAGE_SIZE` | `250` | Apple Music tracks page size. Must be between 1 and 250. |
| `TELEGRAM__PROGRESS_UPDATE_INTERVAL_SECONDS` | `2` | Minimum interval between progress message edits (`0` = every song). |

**Two different concurrency limits:**

- `TELEGRAM__DOWNLOADS__MAX_CONCURRENCY` controls how many download **requests** run at once.
- `DOWNLOADER__MAX_CONCURRENCY` controls how many **provider calls** run at once and how many songs inside one request download in parallel.
- A provider slot cools down for `DOWNLOADER__DELAY_AFTER_DOWNLOAD_SECONDS` after each attempt.

### Telegram API and webhook

| Variable | Default | Description |
|---|---|---|
| `TELEGRAM__SERVER_API_URL` | `https://api.telegram.org` | Telegram API endpoint. Use `http://telegram-bot-api:8081` with the `local-api` profile. |
| `TELEGRAM__WEBHOOK__ENABLED` | `false` | Enable webhook mode. |
| `TELEGRAM__WEBHOOK__URL` | — | Public webhook URL. Required when webhooks are enabled. |
| `TELEGRAM__WEBHOOK__PATH` | `/api/bot/update` | Webhook route. |
| `TELEGRAM__WEBHOOK__SECRET_TOKEN` | — | Optional Telegram secret token. |
| `TELEGRAM__WEBHOOK__MAX_CONNECTIONS` | `50` | Maximum simultaneous webhook connections. |
| `TELEGRAM_API_ID` | — | Local Bot API only. |
| `TELEGRAM_API_HASH` | — | Local Bot API only. |
| `LOGGING__LOGLEVEL__DEFAULT` | `Debug` | Log level. |

Compose mounts are controlled by `BOT_DATA_PATH` (default `./data/bot`) and `TG_DATA_PATH` (default `./data/telegram`).

## How It Works

```mermaid
flowchart LR
    U[Telegram user] --> T[Telegram Bot API]
    T --> H[Update handlers]
    H --> C[Apple Music Catalog]
    H --> Q[Download queue]
    Q --> W[Download workers]
    W --> S[Song sender]
    S --> K[(SQLite + storage chat)]
    S --> L[Provider limiter]
    L --> G[gamdl bridge]
    L --> Y[yt-dlp bridge]
```

1. Telegram delivers a message, inline query, or callback to the update handlers.
2. Search results, metadata, and cache IDs always come from the Apple Music Catalog API.
3. Downloads are placed in a queue. Each request is processed by a worker; songs inside a request run in parallel up to `DOWNLOADER__MAX_CONCURRENCY`.
4. The cache is checked first. On a miss, the selected provider produces the audio file.
5. The file is uploaded to the storage chat, cached in SQLite, and sent to the user.
6. Failed tracks are reported and are not cached, so a later request can retry them.

Finished batches are sent in the original track order. The progress message shows `Songs processed: X of Y` and adds `(N failed)` only when at least one track failed.

## Usage

### Inline search

```text
@YourBotName Imagine Dragons
@YourBotName song:Bohemian Rhapsody
@YourBotName album:Dark Side of the Moon
@YourBotName artist:The Weeknd
@YourBotName playlist:Chill
```

### Search modes

| Prefix | Mode | Description |
|---|---|---|
| *(none)* or `all:` | All | Songs, albums, artists, and playlists. |
| `song:` | Songs | Songs only. |
| `album:` | Albums | Albums only. |
| `artist:` | Artists | Artists only. |
| `playlist:` | Playlists | Playlists only. |
| `album-songs:` | Album tracks | All songs in an album. |
| `playlist-songs:` | Playlist tracks | All songs in a playlist. |
| `artist-songs:` | Artist tracks | All songs by an artist. |
| `artist-albums:` | Artist albums | All albums by an artist. |
| `artist-playlists:` | Artist playlists | Playlists featuring an artist. |

### Apple Music URLs

Paste a link directly into the chat:

```text
https://music.apple.com/us/album/after-hours/1499378108
https://music.apple.com/us/song/blinding-lights/1499378115
```

The bot detects songs, albums, artists, and playlists.

### Buttons and progress

After a result appears, use the inline buttons to download a song, download a whole album/playlist, or browse an artist. Albums and playlists are queued in batches of up to 250 tracks; when more tracks are available, use the **Load next 250 songs** button to continue. While a bulk download runs, the status message shows the queue position and progress. A **Cancel** button stops the request; tracks already downloaded are still sent.

### Commands

| Command | Description |
|---|---|
| `/start` | Welcome message with quick actions. |

## Troubleshooting

| Symptom | Fix |
|---|---|
| Apple Music search fails with `401` | Refresh `APPLE_MUSIC__API_TOKEN`. |
| `gamdl` download fails | Check the Apple Music subscription and `apple_cookies.txt`. |
| `yt-dlp`: `Sign in to confirm you're not a bot` | Refresh YouTube cookies, use a dedicated account, and make sure the browser and bot use the same egress IP. A flagged datacenter IP may still need a proxy. |
| YouTube cookies expire after a while | Replace `youtube_cookies.txt` with a fresh export. The bridge persists rotation, but sessions can still be invalidated. |
| `telegram-bot-api:8081` name not known | Use the public API, or start the `local-api` profile and set `TELEGRAM__SERVER_API_URL`. |
| Webhook is not configured | Check `TELEGRAM__WEBHOOK__URL` reachability, the secret token, and that port `8080` is exposed. Long polling works without a webhook. |
| Queue is full | Wait for pending downloads or raise `TELEGRAM__DOWNLOADS__MAX_PENDING_SONGS`. |
| Duplicate downloads | Run only one bot instance. |

## Project Layout

```text
.
├── src/
│   ├── python/
│   │   ├── gamdl-bridge.py
│   │   └── ytdlp-bridge.py
│   └── frytech.AppleMusicTools.Downloader.TelegramBot/
│       ├── Configuration/
│       ├── Services/          # Caching, Catalog, Downloads, Requests, SongFiles
│       ├── Telegram/
│       └── Program.cs
├── data/
│   ├── bot/                   # app.db, apple_cookies.txt, youtube_cookies.txt
│   └── telegram/              # local Bot API data
├── .env.example
├── docker-compose.yml
├── docker-compose.override.yml
├── Dockerfile
└── LICENSE
```

## License

MIT. See [LICENSE](LICENSE).

This project is not affiliated with Apple Inc. Use it only for content you are allowed to download, and respect the terms of service and copyright law.
