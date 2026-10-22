using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models.Requests;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Catalog;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Requests.Presentation;
using frytech.AppleMusicTools.Downloader.TelegramBot.Telegram;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Requests;

public sealed class InlineQueryHandler
{
    private readonly ITelegramBotClient _client;
    private readonly AppleMusicCatalog _catalog;
    private readonly InlineQueryResultFactory _inlineQueryResultFactory;
    private readonly AppSettings _appSettings;
    private readonly ILogger<InlineQueryHandler> _logger;

    public InlineQueryHandler(
        ITelegramBotClient client,
        AppleMusicCatalog catalog,
        InlineQueryResultFactory inlineQueryResultFactory,
        IOptions<AppSettings> appSettings,
        ILogger<InlineQueryHandler> logger)
    {
        _client = client;
        _catalog = catalog;
        _inlineQueryResultFactory = inlineQueryResultFactory;
        _appSettings = appSettings.Value;
        _logger = logger;
    }

    public async Task HandleAsync(InlineQuery inlineQuery, CancellationToken cancellationToken)
    {
        var request = InlineQueryParser.Parse(inlineQuery.Query, inlineQuery.Offset);

        if (request is null)
        {
            _logger.LogDebug("Ignoring empty inline query {InlineQueryId}", inlineQuery.Id);

            return;
        }

        _logger.LogDebug(
            "Handling inline query {InlineQueryId}: type {QueryType}, payload {Payload}, offset {Offset}",
            inlineQuery.Id,
            request.QueryType,
            request.Payload,
            request.Offset);

        try
        {
            var (results, resultCount) = await FetchAsync(request, cancellationToken);
            var inlineResults = BuildResults(results);
            var nextOffset = InlineQueryParser.GetNextOffset(request.Offset, resultCount);

            _logger.LogDebug("Inline query {InlineQueryId} returned {Count} results", inlineQuery.Id, inlineResults.Count);

            await TelegramApiRetry.ExecuteAsync(
                requestToken => _client.AnswerInlineQuery(
                    inlineQuery.Id,
                    inlineResults,
                    nextOffset: InlineQueryParser.MapOffsetToString(nextOffset),
                    cancellationToken: requestToken),
                _logger,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to handle inline query {InlineQueryId}", inlineQuery.Id);

            try
            {
                await _client.AnswerInlineQuery(inlineQuery.Id, Array.Empty<InlineQueryResult>(), cancellationToken: cancellationToken);
            }
            catch (Exception answerException)
            {
                _logger.LogWarning(answerException, "Failed to answer failed inline query {InlineQueryId}", inlineQuery.Id);
            }
        }
    }

    private IReadOnlyList<InlineQueryResult> BuildResults(IReadOnlyList<Resource> resources)
    {
        var results = new List<InlineQueryResult>(resources.Count);

        foreach (var resource in resources)
        {
            try
            {
                results.Add(_inlineQueryResultFactory.Provide(resource));
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Skipping unsupported inline result {ResourceType} {ResourceId}",
                    resource.ResourceType,
                    resource.Id);
            }
        }

        return results;
    }

    private async Task<(IReadOnlyList<Resource> Results, int Count)> FetchAsync(InlineQueryRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

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
