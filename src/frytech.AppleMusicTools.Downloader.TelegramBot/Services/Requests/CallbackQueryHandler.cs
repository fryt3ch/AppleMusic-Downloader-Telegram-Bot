using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models.Requests;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Catalog;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Downloads;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Requests.Presentation;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Requests;

public sealed class CallbackQueryHandler
{
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
        try
        {
            await _client.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to answer callback query {CallbackQueryId}", callbackQuery.Id);
        }

        var message = callbackQuery.Message;
        var data = callbackQuery.Data;

        if (message is null || data is null || !CallbackData.TryParse(data, out var callbackData))
        {
            _logger.LogWarning("Ignoring callback query {CallbackQueryId} with invalid payload {Data}", callbackQuery.Id, data);

            return;
        }

        var store = _appSettings.AppleMusic.DefaultStore;
        var replyParameters = new ReplyParameters { AllowSendingWithoutReply = true, MessageId = message.MessageId };

        _logger.LogInformation(
            "Handling callback {CallbackAction} for query {CallbackQueryId} in chat {ChatId}",
            callbackData.GetType().Name,
            callbackQuery.Id,
            message.Chat.Id);

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
                    await _downloadCoordinator.StartBulkAsync(message.Chat, callbackQuery.From, [song.Id], store, replyParameters, cancellationToken);
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
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to handle callback {CallbackQueryId} with payload {Data}", callbackQuery.Id, data);

            try
            {
                await _client.SendMessage(
                    message.Chat.Id,
                    "Couldn't load this item. Please try again later.",
                    replyParameters: replyParameters,
                    cancellationToken: cancellationToken);
            }
            catch (Exception sendException)
            {
                _logger.LogWarning(sendException, "Failed to report callback error to chat {ChatId}", message.Chat.Id);
            }
        }
    }

    private async Task<IReadOnlyCollection<string>> GetAlbumSongIdsAsync(string albumId, string store)
    {
        var tracks = await _catalog.GetAlbumTracksAsync(offset: 0, limit: 250, albumId, store);

        return tracks.Where(resource => resource.ResourceType is ResourceType.Songs).Select(resource => resource.Id).ToArray();
    }

    private async Task<IReadOnlyCollection<string>> GetPlaylistSongIdsAsync(string playlistId, string store)
    {
        var tracks = await _catalog.GetPlaylistTracksAsync(offset: 0, limit: 250, playlistId, store);

        return tracks.Where(resource => resource.ResourceType is ResourceType.Songs).Select(resource => resource.Id).ToArray();
    }
}
