import io
import os
import re
import sys
from difflib import SequenceMatcher
from pathlib import Path

import yt_dlp

SEARCH_RESULT_LIMIT = 5

_NOISE_PATTERN = re.compile(
    r"\((?:[^)]*)\)|\[(?:[^\]]*)\]",
    flags=re.IGNORECASE,
)
_NOISE_WORDS = {
    "official", "audio", "video", "lyrics", "lyric", "hd", "hq", "4k",
    "remaster", "remastered", "mv", "m", "explicit", "visualizer",
    "music", "clip", "full", "song",
}


def normalize(text):
    if not text:
        return ""

    text = _NOISE_PATTERN.sub(" ", text).lower()
    text = re.sub(r"[^a-z0-9\s]", " ", text)

    tokens = [token for token in text.split() if token and token not in _NOISE_WORDS]

    return " ".join(tokens)


def score_candidate(candidate, expected_artist, expected_title, expected_duration):
    score = 0.0

    candidate_duration = candidate.get("duration")
    if expected_duration and expected_duration > 0 and candidate_duration:
        difference = abs(candidate_duration - expected_duration)
        if difference <= 3:
            score += 50
        elif difference <= 10:
            score += 30
        elif difference <= 20:
            score += 10
        else:
            score -= 50

    expected_text = normalize(f"{expected_artist} {expected_title}")
    candidate_text = normalize(candidate.get("title") or "")
    if expected_text and candidate_text:
        score += SequenceMatcher(None, candidate_text, expected_text).ratio() * 40

    channel = normalize(candidate.get("channel") or candidate.get("uploader") or "")
    normalized_artist = normalize(expected_artist)
    if normalized_artist and normalized_artist in channel:
        score += 15
    if channel.endswith("topic"):
        score += 10

    return score


def pick_best(entries, expected_artist, expected_title, expected_duration):
    if not entries:
        return None

    return max(
        entries,
        key=lambda entry: score_candidate(entry, expected_artist, expected_title, expected_duration),
    )


def print_error(error):
    message = str(error)
    print(message if message.startswith("ERROR:") else f"ERROR: {message}")


def main():
    if len(sys.argv) < 5:
        print("ERROR: Missing arguments")
        return

    artist = sys.argv[1]
    title = sys.argv[2]

    try:
        expected_duration = float(sys.argv[3])
    except ValueError:
        expected_duration = 0.0

    custom_output_path = Path(sys.argv[4]).resolve()
    output_dir = str(custom_output_path.parent)
    output_stem = custom_output_path.stem

    cookies_path = sys.argv[5].strip() if len(sys.argv) > 5 else ""
    cookie_text = None
    if cookies_path and cookies_path != "-" and os.path.isfile(cookies_path):
        with open(cookies_path, encoding="utf-8") as cookies_file:
            cookie_text = cookies_file.read()

    def build_opts():
        opts = {
            "quiet": True,
            "no_warnings": True,
            "noplaylist": True,
            "js_runtimes": {"deno": {}},
            "extractor_args": {
                "youtube": {
                    # Avoid TVHTML5 clients (tv/tv_simply/tv_downgraded): they use a JS
                    # variant yt-dlp-ejs cannot solve yet, causing "The page needs to be
                    # reloaded" / UNPLAYABLE. See yt-dlp issue #17389.
                    "player_client": ["default", "web_embedded"],
                },
            },
        }
        if cookie_text is not None:
            opts["cookiefile"] = io.StringIO(cookie_text)
        return opts

    query = f"{artist} - {title}".strip()

    try:
        with yt_dlp.YoutubeDL(build_opts()) as ydl:
            search_result = ydl.extract_info(f"ytsearch{SEARCH_RESULT_LIMIT}:{query}", download=False)
    except Exception as e:
        print_error(e)
        return

    candidates = [entry for entry in (search_result or {}).get("entries") or [] if entry]
    best = pick_best(candidates, artist, title, expected_duration)

    if best is None:
        print("NOT_FOUND: no search results")
        return

    video_url = best.get("webpage_url") or f"https://www.youtube.com/watch?v={best.get('id')}"

    download_opts = build_opts()
    download_opts.update({
        "format": "bestaudio/best",
        "outtmpl": str(custom_output_path.with_suffix("")),
        "postprocessors": [
            {"key": "FFmpegExtractAudio", "preferredcodec": "m4a"},
            {"key": "FFmpegMetadata"},
        ],
    })

    try:
        with yt_dlp.YoutubeDL(download_opts) as ydl:
            ydl.extract_info(video_url, download=True)
    except Exception as e:
        print_error(e)
        return

    if not custom_output_path.exists():
        candidates_on_disk = list(Path(output_dir).glob(f"{output_stem}.*"))
        candidates_on_disk = [
            c for c in candidates_on_disk if c.suffix in (".m4a", ".mp4", ".webm", ".aac", ".opus")
        ]
        if candidates_on_disk:
            os.replace(candidates_on_disk[0], custom_output_path)

    if not custom_output_path.exists():
        print(f"ERROR: Output file was not produced at {custom_output_path}")
        return

    print("SUCCESS")


if __name__ == "__main__":
    main()
