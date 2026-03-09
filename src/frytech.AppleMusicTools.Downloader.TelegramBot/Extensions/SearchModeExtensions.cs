using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Extensions;

public static class SearchModeExtensions
{
    public static IEnumerable<ResourceType> GetResourceTypes(this SearchMode searchMode)
    {
        switch (searchMode)
        {
            case SearchMode.All:
                yield return ResourceType.Songs;
                yield return ResourceType.Albums;
                yield return ResourceType.Artists;
                yield return ResourceType.Playlists;
                break;
            
            case SearchMode.Songs:
                yield return ResourceType.Songs;
                break;
            
            case SearchMode.Albums:
                yield return ResourceType.Albums;
                break;
            
            case SearchMode.Playlists:
                yield return ResourceType.Playlists;
                break;
            
            case SearchMode.Authors:
                yield return ResourceType.Artists;
                break;
            
            default:
                throw new InvalidOperationException($"SearchMode {searchMode} is not supported");
        }
    }
}