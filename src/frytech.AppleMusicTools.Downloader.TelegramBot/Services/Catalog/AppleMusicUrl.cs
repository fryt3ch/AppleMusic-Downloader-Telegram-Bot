using System.Text.RegularExpressions;
using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Catalog;

public static partial class AppleMusicUrl
{
    private const string Host = "music.apple.com";

    private static readonly IReadOnlyDictionary<ResourceType, string> ResourceTypeKeywords = new Dictionary<ResourceType, string>()
    {
        [ResourceType.Songs] = "song",
        [ResourceType.MusicVideos] = "music-video",
        [ResourceType.Albums] = "album",
        [ResourceType.Artists] = "artist",
        [ResourceType.Playlists] = "playlist",
    };

    private static readonly IReadOnlyDictionary<string, ResourceType> MediaTypeResourceTypes =
        new Dictionary<string, ResourceType>(StringComparer.OrdinalIgnoreCase)
        {
            ["song"] = ResourceType.Songs,
            ["album"] = ResourceType.Albums,
            ["artist"] = ResourceType.Artists,
            ["playlist"] = ResourceType.Playlists,
        };

    [GeneratedRegex(@"^/(?:[a-z]+/)?(?<mediaType>playlist|album|song|artist)(?:/[^/]+)*/(?<mainId>[^/?]+)(?:\?(?:.*?&)?i=(?<songId>\d+).*)?/?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MediaUrlRegex();

    public static bool TryParse(Uri uri, out MusicElementReference? reference)
    {
        reference = null;

        if (!string.Equals(uri.Host, Host, StringComparison.OrdinalIgnoreCase))
            return false;

        var match = MediaUrlRegex().Match(uri.AbsolutePath + uri.Query);

        if (!match.Success)
            return false;

        var songId = match.Groups["songId"].Success ? match.Groups["songId"].Value : null;

        if (!string.IsNullOrWhiteSpace(songId))
        {
            reference = new MusicElementReference(ResourceType.Songs, songId);

            return true;
        }

        if (!MediaTypeResourceTypes.TryGetValue(match.Groups["mediaType"].Value, out var resourceType))
            return false;

        reference = new MusicElementReference(resourceType, match.Groups["mainId"].Value);

        return true;
    }

    public static Uri Create(ResourceType type, string id, string store)
    {
        return new Uri($"https://{Host}/{store}/{ResourceTypeKeywords[type]}/{id}");
    }
}
