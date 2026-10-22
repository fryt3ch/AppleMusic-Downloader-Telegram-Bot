using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models.Requests;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Catalog;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Requests.Presentation;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Requests;

public sealed class InlineQueryHandler
{
    private readonly ITelegramBotClient _client;
    private readonly AppleMusicCatalog _catalog;
    private readonly InlineQueryResultFactory _inlineQueryResultFactory;
    private readonly AppSettings _appSettings;

    public InlineQueryHandler(
        ITelegramBotClient client,
        AppleMusicCatalog catalog,
        InlineQueryResultFactory inlineQueryResultFactory,
        IOptions<AppSettings> appSettings)
    {
        _client = client;
        _catalog = catalog;
        _inlineQueryResultFactory = inlineQueryResultFactory;
        _appSettings = appSettings.Value;
    }

    public async Task HandleAsync(InlineQuery inlineQuery, CancellationToken cancellationToken)
    {
        var request = InlineQueryParser.Parse(inlineQuery.Query, inlineQuery.Offset);

        if (request is null)
            return;

        var (results, resultCount) = await FetchAsync(request, cancellationToken);
        var nextOffset = InlineQueryParser.GetNextOffset(request.Offset, resultCount);

        await _client.AnswerInlineQuery(
            inlineQuery.Id,
            results.Select(_inlineQueryResultFactory.Provide),
            nextOffset: InlineQueryParser.MapOffsetToString(nextOffset),
            cancellationToken: cancellationToken);
    }

    private async Task<(IReadOnlyList<Resource> Results, int Count)> FetchAsync(InlineQueryRequest request, CancellationToken cancellationToken)
    {
        var store = _appSettings.AppleMusic.DefaultStore;
        var limit = InlineQueryParser.ResultLimit;

        if (IsListQuery(request.QueryType))
        {
            IEnumerable<Resource> resources = request.QueryType switch
            {
                QueryType.ListAlbumSongs => await _catalog.GetAlbumTracksAsync(request.Offset, limit, request.Payload, store),
                QueryType.ListPlaylistSongs => await _catalog.GetPlaylistTracksAsync(request.Offset, limit, request.Payload, store),
                QueryType.ListAuthorSongs => await _catalog.GetAuthorTracksAsync(request.Offset, limit, request.Payload, store),
                QueryType.ListAuthorAlbums => await _catalog.GetAuthorAlbumsAsync(request.Offset, limit, request.Payload, store),
                QueryType.ListAuthorPlaylists => await _catalog.GetAuthorPlaylistsAsync(request.Offset, limit, request.Payload, store),

                _ => throw new InvalidOperationException($"Unsupported list query type: {request.QueryType}."),
            };

            var resourceList = resources.ToList();

            return (resourceList, resourceList.Count);
        }

        var searchMode = ToSearchMode(request.QueryType);
        var searchResults = (await _catalog.SearchAsync(request.Payload, request.Offset, limit, searchMode, store)).ToArray();

        return (searchResults, searchResults.Length);
    }

    private static bool IsListQuery(QueryType queryType)
    {
        return queryType is QueryType.ListAlbumSongs
            or QueryType.ListPlaylistSongs
            or QueryType.ListAuthorSongs
            or QueryType.ListAuthorAlbums
            or QueryType.ListAuthorPlaylists;
    }

    private static SearchMode ToSearchMode(QueryType queryType)
    {
        return queryType switch
        {
            QueryType.SearchAll => SearchMode.All,
            QueryType.SearchSongs => SearchMode.Songs,
            QueryType.SearchAlbums => SearchMode.Albums,
            QueryType.SearchAuthors => SearchMode.Authors,
            QueryType.SearchPlaylists => SearchMode.Playlists,

            _ => throw new InvalidOperationException($"Unsupported search query type: {queryType}."),
        };
    }
}
