using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models.Requests;
using frytech.AppleMusicTools.Downloader.TelegramBot.Resources;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Catalog;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Downloads;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Requests.Presentation;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Requests;

public sealed class CallbackQueryHandler
{
    private const int BatchSongsMaxCount = 500;
    private const int BatchSongsPageSize = 250;
    
    private readonly ITelegramBotClient _client;
    private readonly AppleMusicCatalog _catalog;
    private readonly MusicPresenter _musicPresenter;
    private readonly SongDownloadCoordinator _downloadCoordinator;
    private readonly AppSettings _appSettings;
    private readonly ILogger<CallbackQueryHandler> _logger;

    public CallbackQueryHandler(
        ITelegramBotClient client,
        AppleMusicCatalog catalog,
        MusicPresenter musicPresenter,
        SongDownloadCoordinator downloadCoordinator,
        IOptions<AppSettings> appSettings,
        ILogger<CallbackQueryHandler> logger)
    {
        _client = client;
        _catalog = catalog;
        _musicPresenter = musicPresenter;
        _downloadCoordinator = downloadCoordinator;
        _appSettings = appSettings.Value;
        _logger = logger;
    }

    public async Task HandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var message = callbackQuery.Message;
        var data = callbackQuery.Data;

        if (message is null || data is null || !CallbackData.TryParse(data, out var callbackData))
        {
            _logger.LogWarning("Ignoring callback query {CallbackQueryId} with invalid payload {Data}", callbackQuery.Id, data);

            await TryAnswerCallbackAsync(callbackQuery.Id, null, cancellationToken);

            return;
        }

        if (callbackData is CallbackData.CancelDownload cancelDownload)
        {
            var result = await _downloadCoordinator.CancelAsync(cancelDownload.DownloadId, callbackQuery.From.Id, cancellationToken);

            _logger.LogInformation("Cancel request {DownloadId} by user {UserId}: {Result}", cancelDownload.DownloadId, callbackQuery.From.Id, result.Message);

            await TryAnswerCallbackAsync(callbackQuery.Id, result.Message, cancellationToken);

            return;
        }

        await TryAnswerCallbackAsync(callbackQuery.Id, null, cancellationToken);

        var store = _appSettings.AppleMusic.DefaultStore;
        var replyParameters = new ReplyParameters { AllowSendingWithoutReply = true, MessageId = message.MessageId };

        _logger.LogInformation("Handling callback {CallbackAction} for query {CallbackQueryId} in chat {ChatId}", callbackData.GetType().Name, callbackQuery.Id, message.Chat.Id);

        try
        {
            switch (callbackData)
            {
                case CallbackData.ShowArtist artist:
                    await _musicPresenter.ShowArtistAsync(artist.Id, store, message.Chat, contextMessage: message, replyParameters: null, cancellationToken);
                    break;

                case CallbackData.ShowAlbum album:
                    await _musicPresenter.ShowAlbumAsync(album.Id, store, message.Chat, contextMessage: message, replyParameters: null, cancellationToken);
                    break;

                case CallbackData.DownloadSong song:
                    await _downloadCoordinator.StartSingleAsync(message.Chat, callbackQuery.From, song.Id, store, replyParameters, cancellationToken);
                    break;

                case CallbackData.DownloadAlbum album:
                    var albumSongIds = await GetAlbumSongIdsAsync(album.Id, store);
                    await _downloadCoordinator.StartBulkAsync(message.Chat, callbackQuery.From, albumSongIds, store, replyParameters, cancellationToken);
                    break;

                case CallbackData.DownloadPlaylist playlist:
                    var playlistSongIds = await GetPlaylistSongIdsAsync(playlist.Id, store);
                    await _downloadCoordinator.StartBulkAsync(message.Chat, callbackQuery.From, playlistSongIds, store, replyParameters, cancellationToken);
                    break;

                default:
                    _logger.LogWarning("Unsupported callback payload {Data}", data);
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle callback {CallbackQueryId} with payload {Data}", callbackQuery.Id, data);

            try
            {
                await _client.SendMessage(message.Chat.Id, TelegramMessages.ItemLoadFailed, replyParameters: replyParameters, cancellationToken: cancellationToken);
            }
            catch (Exception sendEx)
            {
                _logger.LogWarning(sendEx, "Failed to report callback error to chat {ChatId}", message.Chat.Id);
            }
        }
    }

    private async Task TryAnswerCallbackAsync(string callbackQueryId, string? text, CancellationToken cancellationToken)
    {
        try
        {
            await _client.AnswerCallbackQuery(callbackQueryId, text: text, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to answer callback query {CallbackQueryId}", callbackQueryId);
        }
    }

    private Task<IReadOnlyCollection<string>> GetAlbumSongIdsAsync(string albumId, string store)
    {
        return GetSongsIdsAsync((offset, limit) => _catalog.GetAlbumTracksAsync(offset, limit, albumId, store));
    }

    private Task<IReadOnlyCollection<string>> GetPlaylistSongIdsAsync(string playlistId, string store)
    {
        return GetSongsIdsAsync((offset, limit) => _catalog.GetPlaylistTracksAsync(offset, limit, playlistId, store));
    }

    private async Task<IReadOnlyCollection<string>> GetSongsIdsAsync(Func<int, int, Task<IEnumerable<Resource>>> tracksProvider)
    {
        var result = new HashSet<string>();
        var offset = 0;

        while (result.Count < BatchSongsMaxCount)
        {
            var tracks = (await tracksProvider(offset, BatchSongsPageSize)).ToList();

            foreach (var track in tracks)
            {
                if (track.ResourceType is not ResourceType.Songs)
                    continue;
                
                result.Add(track.Id);

                if (result.Count >= BatchSongsMaxCount)
                    break;
            }
            
            if (tracks.Count < BatchSongsPageSize)
                break;
            
            offset += BatchSongsPageSize;
        }

        return result;
    }
}
