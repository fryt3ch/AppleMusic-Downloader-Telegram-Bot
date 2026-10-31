import contextlib
import io
import json
import os
import re
import shutil
import sys
import tempfile
import urllib.error
import urllib.request
from difflib import SequenceMatcher
from pathlib import Path

import yt_dlp
from gamdl.interface.enums import MediaRating, MediaType
from gamdl.interface.types import MediaTags
from mutagen.mp4 import MP4, MP4Cover
from yt_dlp.cookies import YoutubeDLCookieJar

try:
    import fcntl
except ImportError:
    fcntl = None

SEARCH_RESULT_LIMIT = 5
TAG_DATE_FORMAT = "%Y-%m-%dT%H:%M:%SZ"

_NOISE_PATTERN = re.compile(
    r"\((?:[^)]*)\)|\[(?:[^\]]*)\]",
    flags=re.IGNORECASE,
)
_NOISE_WORDS = {
    "official", "audio", "video", "lyrics", "lyric", "hd", "hq", "4k",
    "remaster", "remastered", "mv", "m", "explicit", "visualizer",
    "music", "clip", "full", "song",
}


class NoSearchResults(Exception):
    pass


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


def build_opts(cookie_source):
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

    if cookie_source:
        opts["cookiefile"] = cookie_source

    return opts


def _cookie_key(cookie):
    return cookie.domain, cookie.path, cookie.name


def _load_jar_from_text(text):
    jar = YoutubeDLCookieJar()

    if text:
        jar.load(io.StringIO(text))

    return jar


def _load_jar_from_path(path):
    jar = YoutubeDLCookieJar(path)
    jar.load()

    return jar


@contextlib.contextmanager
def cookie_lock(path):
    lock_file = None

    try:
        lock_file = open(path, "a+")
    except OSError as error:
        print(f"WARNING: failed to open cookie lock file: {error}", file=sys.stderr)
        yield
        return

    try:
        if fcntl is not None:
            fcntl.flock(lock_file.fileno(), fcntl.LOCK_EX)

        yield
    finally:
        if fcntl is not None:
            fcntl.flock(lock_file.fileno(), fcntl.LOCK_UN)

        lock_file.close()


def persist_cookie_updates(master_path, working_path, base_text):
    if not os.path.isfile(working_path):
        return

    try:
        base_jar = _load_jar_from_text(base_text)
        ours_jar = _load_jar_from_path(working_path)
    except Exception as error:
        print(f"WARNING: failed to read cookie jars: {error}", file=sys.stderr)
        return

    base = {_cookie_key(cookie): cookie for cookie in base_jar}
    ours = {_cookie_key(cookie): cookie for cookie in ours_jar}

    with cookie_lock(f"{master_path}.lock"):
        if not os.path.isfile(master_path):
            return

        try:
            master_jar = _load_jar_from_path(master_path)
        except Exception as error:
            print(f"WARNING: failed to read master cookies: {error}", file=sys.stderr)
            return

        master = {_cookie_key(cookie): cookie for cookie in master_jar}

        changed = []
        conflicts = []

        for key, our_cookie in ours.items():
            base_cookie = base.get(key)
            master_cookie = master.get(key)

            if base_cookie is not None and our_cookie.value == base_cookie.value:
                continue

            if master_cookie is not None and our_cookie.value == master_cookie.value:
                continue

            if base_cookie is None:
                if master_cookie is None:
                    master_jar.set_cookie(our_cookie)
                    changed.append(our_cookie.name)

                continue

            if master_cookie is None or master_cookie.value == base_cookie.value:
                master_jar.set_cookie(our_cookie)
                changed.append(our_cookie.name)
            else:
                conflicts.append(our_cookie.name)

        if not changed:
            return

        master_dir = os.path.dirname(master_path) or "."
        temp_path = None

        try:
            fd, temp_path = tempfile.mkstemp(prefix=".cookies-", suffix=".tmp", dir=master_dir)
            os.close(fd)

            master_jar.save(temp_path, ignore_discard=True, ignore_expires=True)

            try:
                shutil.copymode(master_path, temp_path)
            except OSError:
                pass

            try:
                os.replace(temp_path, master_path)
                temp_path = None
            except OSError as error:
                print(f"WARNING: atomic cookie replace failed, writing in place: {error}", file=sys.stderr)
                master_jar.save(master_path, ignore_discard=True, ignore_expires=True)
        finally:
            if temp_path and os.path.exists(temp_path):
                try:
                    os.remove(temp_path)
                except OSError:
                    pass

        print(f"COOKIE_UPDATED: {', '.join(sorted(set(changed)))}", file=sys.stderr)

        if conflicts:
            print(f"COOKIE_CONFLICT: {', '.join(sorted(set(conflicts)))}", file=sys.stderr)


class CookieSession:
    def __init__(self, master_path):
        self.master_path = master_path
        self.working_path = None
        self.base_text = None
        self.persist = False

    def __enter__(self):
        if not self.master_path or self.master_path == "-" or not os.path.isfile(self.master_path):
            return None

        try:
            with open(self.master_path, encoding="utf-8") as cookies_file:
                self.base_text = cookies_file.read()
        except OSError as error:
            print(f"WARNING: failed to read cookies: {error}", file=sys.stderr)
            return None

        if not self.base_text.strip():
            return None

        try:
            fd, self.working_path = tempfile.mkstemp(prefix="ytdlp-cookies-", suffix=".txt")

            with os.fdopen(fd, "w", encoding="utf-8") as cookies_file:
                cookies_file.write(self.base_text)
        except OSError as error:
            print(f"WARNING: failed to create cookie working copy: {error}", file=sys.stderr)
            self.working_path = None
            return None

        self.persist = True

        return self.working_path

    def __exit__(self, exc_type, exc, tb):
        if self.persist and self.working_path:
            try:
                persist_cookie_updates(self.master_path, self.working_path, self.base_text)
            except Exception as error:
                print(f"WARNING: failed to persist cookies: {error}", file=sys.stderr)

        if self.working_path:
            try:
                os.remove(self.working_path)
            except OSError:
                pass

        return False


def load_metadata(path):
    with open(path, encoding="utf-8") as metadata_file:
        return json.load(metadata_file)


def build_media_tags(metadata):
    media_type = metadata.get("media_type")
    rating = metadata.get("rating")

    return MediaTags(
        album=metadata.get("album"),
        album_artist=metadata.get("album_artist"),
        album_id=metadata.get("album_id"),
        album_sort=metadata.get("album_sort"),
        artist=metadata.get("artist"),
        artist_id=metadata.get("artist_id"),
        artist_sort=metadata.get("artist_sort"),
        comment=metadata.get("comment"),
        compilation=metadata.get("compilation"),
        composer=metadata.get("composer"),
        composer_id=metadata.get("composer_id"),
        composer_sort=metadata.get("composer_sort"),
        copyright=metadata.get("copyright"),
        date=metadata.get("date"),
        disc=metadata.get("disc"),
        disc_total=metadata.get("disc_total"),
        gapless=metadata.get("gapless"),
        genre=metadata.get("genre"),
        genre_id=metadata.get("genre_id"),
        lyrics=metadata.get("lyrics"),
        media_type=MediaType(media_type) if media_type is not None else None,
        rating=MediaRating(rating) if rating is not None else None,
        storefront=metadata.get("storefront"),
        title=metadata.get("title"),
        title_id=metadata.get("title_id"),
        title_sort=metadata.get("title_sort"),
        track=metadata.get("track"),
        track_total=metadata.get("track_total"),
        xid=metadata.get("xid"),
    )


def format_cover_url(cover_url):
    if not cover_url:
        return None

    return re.sub(r"/\{w\}x\{h\}([a-z]{2})\.jpg", "/1200x1200bb.jpg", cover_url)


def download_cover(cover_url):
    if not cover_url:
        return None

    try:
        with urllib.request.urlopen(cover_url, timeout=30) as response:
            return response.read()
    except urllib.error.HTTPError as error:
        if error.code == 404:
            return None

        raise


def write_tags(path, tags, cover_bytes):
    mp4 = MP4(path)

    if mp4.tags is not None:
        mp4.tags.delete(str(path))
        mp4 = MP4(path)

    if cover_bytes:
        mp4["covr"] = [MP4Cover(cover_bytes, imageformat=MP4Cover.FORMAT_JPEG)]

    mp4.update(tags.as_mp4_tags(TAG_DATE_FORMAT))
    mp4.save()


def download_from_youtube(cookie_source, artist, title, expected_duration, custom_output_path):
    output_dir = str(custom_output_path.parent)
    output_stem = custom_output_path.stem
    query = f"{artist} - {title}".strip()

    with yt_dlp.YoutubeDL(build_opts(cookie_source)) as ydl:
        search_result = ydl.extract_info(f"ytsearch{SEARCH_RESULT_LIMIT}:{query}", download=False)

    candidates = [entry for entry in (search_result or {}).get("entries") or [] if entry]
    best = pick_best(candidates, artist, title, expected_duration)

    if best is None:
        raise NoSearchResults()

    video_url = best.get("webpage_url") or f"https://www.youtube.com/watch?v={best.get('id')}"

    download_opts = build_opts(cookie_source)
    download_opts.update({
        "format": "bestaudio/best",
        "outtmpl": str(custom_output_path.with_suffix("")),
        "postprocessors": [
            {"key": "FFmpegExtractAudio", "preferredcodec": "m4a"},
        ],
    })

    with yt_dlp.YoutubeDL(download_opts) as ydl:
        ydl.extract_info(video_url, download=True)

    if not custom_output_path.exists():
        candidates_on_disk = list(Path(output_dir).glob(f"{output_stem}.*"))
        candidates_on_disk = [
            c for c in candidates_on_disk if c.suffix in (".m4a", ".mp4", ".webm", ".aac", ".opus")
        ]
        if candidates_on_disk:
            os.replace(candidates_on_disk[0], custom_output_path)

    if not custom_output_path.exists():
        raise RuntimeError(f"Output file was not produced at {custom_output_path}")


def run_download(metadata, cookie_source, custom_output_path):
    artist = metadata.get("artist") or ""
    title = metadata.get("title") or ""
    duration = metadata.get("duration_seconds") or 0.0

    download_from_youtube(
        cookie_source,
        artist,
        title,
        float(duration),
        custom_output_path,
    )

    tags = build_media_tags(metadata)
    cover_bytes = download_cover(format_cover_url(metadata.get("cover_url")))

    write_tags(custom_output_path, tags, cover_bytes)

    print("SUCCESS")


def main():
    if len(sys.argv) < 4:
        print("ERROR: Missing arguments")
        return

    metadata_path = sys.argv[1].strip()
    custom_output_path = Path(sys.argv[2]).resolve()
    cookies_path = sys.argv[3].strip() if len(sys.argv) > 3 else ""

    try:
        metadata = load_metadata(metadata_path)
    except Exception as error:
        print_error(error)
        return

    try:
        with CookieSession(cookies_path) as cookie_source:
            run_download(metadata, cookie_source, custom_output_path)
    except NoSearchResults:
        print("NOT_FOUND: no search results")
    except Exception as error:
        print_error(error)


if __name__ == "__main__":
    main()
