import asyncio
import sys
from pathlib import Path
from gamdl.api import AppleMusicApi
from gamdl.downloader import AppleMusicBaseDownloader, AppleMusicDownloader, AppleMusicSongDownloader
from gamdl.interface import AppleMusicBaseInterface, AppleMusicInterface, AppleMusicSongInterface

async def main():
    if len(sys.argv) < 3:
        print("ERROR: Missing arguments")
        return

    url = sys.argv[1]
    custom_output_path = Path(sys.argv[2]).resolve() # Получаем абсолютный путь
    
    output_dir = str(custom_output_path.parent)
    output_filename_template = custom_output_path.stem 

    cookies_path = sys.argv[3] if len(sys.argv) > 3 else "cookies.txt"

    try:
        apple_music_api = await AppleMusicApi.create_from_netscape_cookies(cookies_path=cookies_path)
        if not apple_music_api.active_subscription:
            print(f"ERROR: No active subscription in {cookies_path}")
            return

        base_interface = await AppleMusicBaseInterface.create(apple_music_api=apple_music_api)
        song_interface = AppleMusicSongInterface(base=base_interface)
        interface = AppleMusicInterface(song=song_interface, music_video=None, uploaded_video=None)
        
        # Передаем no_synced_lyrics=True, чтобы не плодить .lrc файлы на диске
        base_downloader = AppleMusicBaseDownloader(
            interface=interface,
            output_path=Path(output_dir),
            no_album_folder_template=output_filename_template
        )
        
        song_downloader = AppleMusicSongDownloader(base=base_downloader)
        downloader = AppleMusicDownloader(song=song_downloader, music_video=None, uploaded_video=None, no_synced_lyrics=True)

        async for media in downloader.get_download_item_from_url(url):
            media.final_path = custom_output_path
            await downloader.download(media)
            
        print("SUCCESS")
    except Exception as e:
        print(f"ERROR: {e}")

if __name__ == "__main__":
    asyncio.run(main())