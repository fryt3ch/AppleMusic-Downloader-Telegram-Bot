using frytech.AppleMusic.API.Extensions;
using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Catalog;
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

    public MusicPresenter(ITelegramBotClient client, AppleMusicCatalog catalog, IOptions<AppSettings> appSettings)
    {
        _client = client;
        _catalog = catalog;
        _appSettings = appSettings.Value;
    }

    public async Task ShowArtistAsync(string artistId, string store, Chat chat, Message? contextMessage, ReplyParameters? replyParameters, CancellationToken cancellationToken)
    {
        var artist = await _catalog.GetArtistAsync(artistId, store);

        var caption = artist.Attributes.Name;
        var replyMarkup = new InlineKeyboardMarkup()
            .AddButton(new InlineKeyboardButton("Songs") { SwitchInlineQueryCurrentChat = $"{InlineQueryParser.ArtistSongsKeyword}: {artist.Id}" })
            .AddButton(new InlineKeyboardButton("Albums") { SwitchInlineQueryCurrentChat = $"{InlineQueryParser.ArtistAlbumsKeyword}: {artist.Id}" })
            .AddButton(new InlineKeyboardButton("Playlists") { SwitchInlineQueryCurrentChat = $"{InlineQueryParser.ArtistPlaylistsKeyword}: {artist.Id}" });

        var artworkUrl = artist.Attributes.Artwork?.GetImageUrl(_appSettings.Telegram.ThumbnailSize);

        await SendCardAsync(chat, contextMessage, caption, artworkUrl, replyMarkup, replyParameters, cancellationToken);
    }

    public async Task ShowAlbumAsync(string albumId, string store, Chat chat, Message? contextMessage, ReplyParameters? replyParameters, CancellationToken cancellationToken)
    {
        var album = await _catalog.GetAlbumAsync(albumId, store);

        var caption = $"{album.Attributes.ArtistName} - {album.Attributes.Name}\n\n{album.Attributes.TrackCount} tracks\n{album.Attributes.ReleaseDate:yyyy.MM.dd}";
        var replyMarkup = new InlineKeyboardMarkup()
            .AddButton(new InlineKeyboardButton("Songs") { SwitchInlineQueryCurrentChat = $"{InlineQueryParser.AlbumSongsKeyword}: {album.Id}" });

        if (album.Relationships.Artists.Data?.FirstOrDefault()?.Id is { } artistId)
            replyMarkup.AddButton(new InlineKeyboardButton("Artist") { CallbackData = CallbackData.ForArtist(artistId) });

        replyMarkup.AddNewRow(new InlineKeyboardButton("Get All Songs") { CallbackData = CallbackData.ForDownloadAlbum(album.Id) });

        var artworkUrl = album.Attributes.Artwork?.GetImageUrl(_appSettings.Telegram.ThumbnailSize);

        await SendCardAsync(chat, contextMessage, caption, artworkUrl, replyMarkup, replyParameters, cancellationToken);
    }

    public async Task ShowPlaylistAsync(string playlistId, string store, Chat chat, Message? contextMessage, ReplyParameters? replyParameters, CancellationToken cancellationToken)
    {
        var playlist = await _catalog.GetPlaylistAsync(playlistId, store);

        var caption = $"{playlist.Attributes.Name}\n\nCurator: {playlist.Attributes.CuratorName}";
        var replyMarkup = new InlineKeyboardMarkup()
            .AddButton(new InlineKeyboardButton("Songs") { SwitchInlineQueryCurrentChat = $"{InlineQueryParser.PlaylistSongsKeyword}: {playlist.Id}" });

        replyMarkup.AddNewRow(new InlineKeyboardButton("Get All Songs") { CallbackData = CallbackData.ForDownloadPlaylist(playlist.Id) });

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
            await _client.DeleteMessage(chat.Id, contextMessage.MessageId, cancellationToken);

        if (artworkUrl is null)
        {
            await _client.SendMessage(
                chat.Id,
                caption,
                replyParameters: replyParameters,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
        }
        else
        {
            await _client.SendPhoto(
                chat.Id,
                InputFile.FromUri(artworkUrl),
                caption: caption,
                replyParameters: replyParameters,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
        }
    }
}
