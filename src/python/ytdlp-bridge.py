import os
import sys
from pathlib import Path

import yt_dlp


def main():
    if len(sys.argv) < 3:
        print("ERROR: Missing arguments")
        return

    query = sys.argv[1]
    custom_output_path = Path(sys.argv[2]).resolve()

    output_dir = str(custom_output_path.parent)
    output_stem = custom_output_path.stem

    cookies_path = sys.argv[3] if len(sys.argv) > 3 else ""
    cookies_path = cookies_path.strip()

    ydl_opts = {
        "format": "bestaudio/best",
        "outtmpl": str(custom_output_path.with_suffix("")),
        "noplaylist": True,
        "quiet": True,
        "no_warnings": True,
        "postprocessors": [
            {"key": "FFmpegExtractAudio", "preferredcodec": "m4a"},
            {"key": "FFmpegMetadata"},
        ],
    }

    if cookies_path and cookies_path != "-" and os.path.isfile(cookies_path):
        ydl_opts["cookiefile"] = cookies_path

    try:
        with yt_dlp.YoutubeDL(ydl_opts) as ydl:
            ydl.extract_info(f"ytsearch1:{query}", download=True)
    except Exception as e:
        print(f"ERROR: {e}")
        return

    produced_path = custom_output_path
    if not produced_path.exists():
        candidates = list(Path(output_dir).glob(f"{output_stem}.*"))
        candidates = [c for c in candidates if c.suffix in (".m4a", ".mp4", ".webm", ".aac", ".opus")]
        if candidates:
            os.replace(candidates[0], custom_output_path)

    if not custom_output_path.exists():
        print(f"ERROR: Output file was not produced at {custom_output_path}")
        return

    print("SUCCESS")


if __name__ == "__main__":
    main()
