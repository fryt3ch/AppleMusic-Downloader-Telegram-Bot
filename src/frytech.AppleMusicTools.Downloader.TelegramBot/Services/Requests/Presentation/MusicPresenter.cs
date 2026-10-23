using frytech.AppleMusic.API.Extensions;
using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models.Requests;
using frytech.AppleMusicTools.Downloader.TelegramBot.Resources;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Catalog;
using frytech.AppleMusicTools.Downloader.TelegramBot.Telegram;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Requests.Presentation;

public sealed class MusicPresenter
{
    private readonly ITelegramBotClient _client;
    private readonly AppleMusicCatalog _catalog;
    private readonly AppSettings _appSettings;
    private readonly ILogger<MusicPresenter> _logger;

    public MusicPresenter(
        ITelegramBotClient client,
        AppleMusicCatalog catalog,
        IOptions<AppSettings> appSettings,
        ILogger<MusicPresenter> logger)
    {
        _client = client;
        _catalog = catalog;
        _appSettings = appSettings.Value;
        _logger = logger;
    }

    public async Task ShowArtistAsync(string artistId, string store, Chat chat, Message? contextMessage, ReplyParameters? replyParameters, CancellationToken cancellationToken)
    {
        var artist = await _catalog.GetArtistAsync(artistId, store);

        var caption = artist.Attributes.Name;
        var replyMarkup = new InlineKeyboardMarkup()
            .AddButton(new InlineKeyboardButton(TelegramButtons.Songs) { SwitchInlineQueryCurrentChat = $"{InlineQueryParser.ArtistSongsKeyword}: {artist.Id}" })
            .AddButton(new InlineKeyboardButton(TelegramButtons.Albums) { SwitchInlineQueryCurrentChat = $"{InlineQueryParser.ArtistAlbumsKeyword}: {artist.Id}" })
            .AddButton(new InlineKeyboardButton(TelegramButtons.Playlists) { SwitchInlineQueryCurrentChat = $"{InlineQueryParser.ArtistPlaylistsKeyword}: {artist.Id}" });

        var artworkUrl = artist.Attributes.Artwork?.GetImageUrl(_appSettings.Telegram.ThumbnailSize);

        await SendCardAsync(chat, contextMessage, caption, artworkUrl, replyMarkup, replyParameters, cancellationToken);
    }

    public async Task ShowAlbumAsync(string albumId, string store, Chat chat, Message? contextMessage, ReplyParameters? replyParameters, CancellationToken cancellationToken)
    {
        var album = await _catalog.GetAlbumAsync(albumId, store);

        var caption = string.Format(
            TelegramMessages.AlbumCaption,
            album.Attributes.ArtistName,
            album.Attributes.Name,
            album.Attributes.TrackCount,
            album.Attributes.ReleaseDate.ToString("yyyy.MM.dd"));

        var replyMarkup = new InlineKeyboardMarkup()
            .AddButton(new InlineKeyboardButton(TelegramButtons.Songs) { SwitchInlineQueryCurrentChat = $"{InlineQueryParser.AlbumSongsKeyword}: {album.Id}" });

        if (album.Relationships.Artists.Data?.FirstOrDefault()?.Id is { } artistId)
            replyMarkup.AddButton(new InlineKeyboardButton(TelegramButtons.Artist) { CallbackData = CallbackData.ForArtist(artistId) });

        replyMarkup.AddNewRow(new InlineKeyboardButton(TelegramButtons.GetAllSongs) { CallbackData = CallbackData.ForDownloadAlbum(album.Id) });

        var artworkUrl = album.Attributes.Artwork?.GetImageUrl(_appSettings.Telegram.ThumbnailSize);

        await SendCardAsync(chat, contextMessage, caption, artworkUrl, replyMarkup, replyParameters, cancellationToken);
    }

    public async Task ShowPlaylistAsync(string playlistId, string store, Chat chat, Message? contextMessage, ReplyParameters? replyParameters, CancellationToken cancellationToken)
    {
        var playlist = await _catalog.GetPlaylistAsync(playlistId, store);

        var caption = string.Format(TelegramMessages.PlaylistCaption, playlist.Attributes.Name, playlist.Attributes.CuratorName);
        var replyMarkup = new InlineKeyboardMarkup()
            .AddButton(new InlineKeyboardButton(TelegramButtons.Songs) { SwitchInlineQueryCurrentChat = $"{InlineQueryParser.PlaylistSongsKeyword}: {playlist.Id}" });

        replyMarkup.AddNewRow(new InlineKeyboardButton(TelegramButtons.GetAllSongs) { CallbackData = CallbackData.ForDownloadPlaylist(playlist.Id) });

        var artworkUrl = playlist.Attributes.Artwork?.GetImageUrl(_appSettings.Telegram.ThumbnailSize);

        await SendCardAsync(chat, contextMessage, caption, artworkUrl, replyMarkup, replyParameters, cancellationToken);
    }

    private async Task SendCardAsync(
        Chat chat,
        Message? contextMessage,
        string caption,
        Uri? artworkUrl,
        InlineKeyboardMarkup replyMarkup,
        ReplyParameters? replyParameters,
        CancellationToken cancellationToken)
    {
        if (contextMessage is not null)
        {
            try
            {
                await _client.DeleteMessage(chat.Id, contextMessage.MessageId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete context message {MessageId} in chat {ChatId}", contextMessage.MessageId, chat.Id);
            }
        }

        _logger.LogDebug("Sending music card to chat {ChatId}", chat.Id);

        if (artworkUrl is null)
        {
            await SendTextCardAsync(chat, caption, replyMarkup, replyParameters, cancellationToken);

            return;
        }

        try
        {
            await TelegramApiRetry.ExecuteAsync(
                requestToken => _client.SendPhoto(
                    chat.Id,
                    InputFile.FromUri(artworkUrl),
                    caption: caption,
                    replyParameters: replyParameters,
                    replyMarkup: replyMarkup,
                    cancellationToken: requestToken),
                _logger,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send photo card in chat {ChatId}; falling back to text", chat.Id);

            await SendTextCardAsync(chat, caption, replyMarkup, replyParameters, cancellationToken);
        }
    }

    private async Task SendTextCardAsync(
        Chat chat,
        string caption,
        InlineKeyboardMarkup replyMarkup,
        ReplyParameters? replyParameters,
        CancellationToken cancellationToken)
    {
        await TelegramApiRetry.ExecuteAsync(
            requestToken => _client.SendMessage(
                chat.Id,
                caption,
                replyParameters: replyParameters,
                replyMarkup: replyMarkup,
                cancellationToken: requestToken),
            _logger,
            cancellationToken);
    }
}
