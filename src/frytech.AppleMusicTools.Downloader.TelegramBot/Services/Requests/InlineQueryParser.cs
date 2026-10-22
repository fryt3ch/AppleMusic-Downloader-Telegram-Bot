using System.Text.RegularExpressions;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models.Requests;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Requests;

public static partial class InlineQueryParser
{
    public const int ResultLimit = 10;

    public const string AllKeyword = "all";
    public const string SongKeyword = "song";
    public const string AlbumKeyword = "album";
    public const string ArtistKeyword = "artist";
    public const string PlaylistKeyword = "playlist";
    public const string AlbumSongsKeyword = "album-songs";
    public const string PlaylistSongsKeyword = "playlist-songs";
    public const string ArtistSongsKeyword = "artist-songs";
    public const string ArtistAlbumsKeyword = "artist-albums";
    public const string ArtistPlaylistsKeyword = "artist-playlists";

    private static readonly IReadOnlyDictionary<string, QueryType> Keywords =
        new Dictionary<string, QueryType>(StringComparer.OrdinalIgnoreCase)
        {
            [AllKeyword] = QueryType.SearchAll,
            [SongKeyword] = QueryType.SearchSongs,
            [AlbumKeyword] = QueryType.SearchAlbums,
            [ArtistKeyword] = QueryType.SearchAuthors,
            [PlaylistKeyword] = QueryType.SearchPlaylists,
            [AlbumSongsKeyword] = QueryType.ListAlbumSongs,
            [PlaylistSongsKeyword] = QueryType.ListPlaylistSongs,
            [ArtistSongsKeyword] = QueryType.ListAuthorSongs,
            [ArtistAlbumsKeyword] = QueryType.ListAuthorAlbums,
            [ArtistPlaylistsKeyword] = QueryType.ListAuthorPlaylists,
        };

    [GeneratedRegex(@"^(.+):\s*(.*)$")]
    private static partial Regex QueryRegex();

    public static InlineQueryRequest? Parse(string query, string? offset)
    {
        query = query.Trim();

        if (query.Length == 0)
            return null;

        var parsedOffset = int.TryParse(offset, out var offsetValue) ? offsetValue : 0;

        var queryType = QueryType.SearchAll;
        var payload = query;

        var match = QueryRegex().Match(query);

        if (match.Success && Keywords.TryGetValue(match.Groups[1].Value, out var mappedQueryType))
        {
            queryType = mappedQueryType;
            payload = match.Groups[2].Value.Trim();
        }

        if (payload.Length == 0)
            return null;

        return new InlineQueryRequest(payload, queryType, parsedOffset);
    }

    public static int GetNextOffset(int offset, int resultCount)
    {
        return resultCount >= ResultLimit ? offset + resultCount : 0;
    }

    public static string? MapOffsetToString(int offset)
    {
        return offset > 0 ? offset.ToString() : null;
    }
}
