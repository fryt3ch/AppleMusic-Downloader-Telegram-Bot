using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models.Downloads;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models.Requests;
using frytech.AppleMusicTools.Downloader.TelegramBot.Resources;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Catalog;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Downloads;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Requests.Presentation;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Requests;

public sealed class CallbackQueryHandler
{
    private readonly ITelegramBotClient _client;
    private readonly AppleMusicCatalog _catalog;
    private readonly MusicPresenter _musicPresenter;
    private readonly SongDownloadCoordinator _downloadCoordinator;
    private readonly AppSettings _appSettings;
    private readonly int _maxSongsPerBatch;
    private readonly int _songsPageSize;
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
        _maxSongsPerBatch = _appSettings.Telegram.Downloads.MaxSongsPerBatch;
        _songsPageSize = _appSettings.Telegram.Downloads.SongsPageSize;
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
        var sourceMessageId = callbackData switch
        {
            CallbackData.DownloadAlbum { Offset: > 0 } => message.ReplyToMessage?.MessageId,
            CallbackData.DownloadPlaylist { Offset: > 0 } => message.ReplyToMessage?.MessageId,
            _ => message.MessageId,
        };

        var replyParameters = sourceMessageId is { } sourceId
            ? new ReplyParameters { AllowSendingWithoutReply = true, MessageId = sourceId }
            : null;

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
                    var albumBatch = await GetAlbumBatchAsync(album.Id, store, album.Offset);

                    await EnqueueBatchAsync(
                        message.Chat,
                        callbackQuery.From,
                        albumBatch,
                        store,
                        nextOffset => CallbackData.ForDownloadAlbum(album.Id, nextOffset),
                        album.Offset,
                        message,
                        replyParameters,
                        cancellationToken);

                    break;

                case CallbackData.DownloadPlaylist playlist:
                    var playlistBatch = await GetPlaylistBatchAsync(playlist.Id, store, playlist.Offset);

                    await EnqueueBatchAsync(
                        message.Chat,
                        callbackQuery.From,
                        playlistBatch,
                        store,
                        nextOffset => CallbackData.ForDownloadPlaylist(playlist.Id, nextOffset),
                        playlist.Offset,
                        message,
                        replyParameters,
                        cancellationToken);

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

    private Task<SongBatch> GetAlbumBatchAsync(string albumId, string store, int offset)
    {
        return GetSongsBatchAsync(
            (pageOffset, limit) => _catalog.GetAlbumTracksPageAsync(pageOffset, limit, albumId, store),
            offset);
    }

    private Task<SongBatch> GetPlaylistBatchAsync(string playlistId, string store, int offset)
    {
        return GetSongsBatchAsync(
            (pageOffset, limit) => _catalog.GetPlaylistTracksPageAsync(pageOffset, limit, playlistId, store),
            offset);
    }

    private async Task<SongBatch> GetSongsBatchAsync(Func<int, int, Task<TrackPage>> tracksProvider, int offset)
    {
        var songIds = new List<string>(_maxSongsPerBatch);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var nextOffset = offset;
        var hasMore = false;

        while (songIds.Count < _maxSongsPerBatch)
        {
            var page = await tracksProvider(nextOffset, _songsPageSize);

            if (page.Items.Count == 0)
                break;

            var index = 0;

            for (; index < page.Items.Count && songIds.Count < _maxSongsPerBatch; index++)
            {
                var track = page.Items[index];

                nextOffset++;

                if (track.ResourceType is ResourceType.Songs && seen.Add(track.Id))
                    songIds.Add(track.Id);
            }

            hasMore = index < page.Items.Count || page.HasMore;

            if (!hasMore || songIds.Count >= _maxSongsPerBatch)
                break;
        }

        return new SongBatch(songIds, nextOffset, hasMore);
    }

    private async Task EnqueueBatchAsync(
        Chat chat,
        User user,
        SongBatch batch,
        string store,
        Func<int, string> nextCallback,
        int currentOffset,
        Message sourceMessage,
        ReplyParameters? replyParameters,
        CancellationToken cancellationToken)
    {
        if (batch.SongIds.Count == 0)
        {
            var noSongsText = TelegramMessages.BatchNoSongs;

            if (currentOffset > 0)
            {
                await TryEditBatchMessageAsync(chat, sourceMessage.MessageId, noSongsText, new InlineKeyboardMarkup(), cancellationToken);
            }
            else
            {
                await _client.SendMessage(chat.Id, noSongsText, replyParameters: replyParameters, cancellationToken: cancellationToken);
            }

            return;
        }

        var text = string.Format(
            batch.HasMore ? TelegramMessages.BatchQueuedMore : TelegramMessages.BatchQueuedAll,
            batch.SongIds.Count);

        var replyMarkup = batch.HasMore
            ? new InlineKeyboardMarkup().AddButton(
                new InlineKeyboardButton(string.Format(TelegramButtons.DownloadNextBatch, _maxSongsPerBatch))
                {
                    CallbackData = nextCallback(batch.NextOffset),
                })
            : new InlineKeyboardMarkup();

        var notice = new DownloadBatchNotice(text, replyMarkup, replyParameters, currentOffset > 0 ? sourceMessage.MessageId : null);

        await _downloadCoordinator.StartBulkAsync(chat, user, batch.SongIds, store, replyParameters, notice, cancellationToken);
    }

    private async Task<bool> TryEditBatchMessageAsync(Chat chat, int messageId, string text, InlineKeyboardMarkup replyMarkup, CancellationToken cancellationToken)
    {
        try
        {
            await _client.EditMessageText(chat.Id, messageId, text, replyMarkup: replyMarkup, cancellationToken: cancellationToken);

            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to edit batch message {MessageId} in chat {ChatId}", messageId, chat.Id);

            return false;
        }
    }

    private sealed record SongBatch(IReadOnlyList<string> SongIds, int NextOffset, bool HasMore);
}
