using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Resources;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Catalog;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Downloads;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Requests.Presentation;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Requests;

public sealed class MessageHandler
{
    private readonly ITelegramBotClient _client;
    private readonly MusicPresenter _musicPresenter;
    private readonly SongDownloadCoordinator _downloadCoordinator;
    private readonly AppSettings _appSettings;

    public MessageHandler(
        ITelegramBotClient client,
        MusicPresenter musicPresenter,
        SongDownloadCoordinator downloadCoordinator,
        IOptions<AppSettings> appSettings)
    {
        _client = client;
        _musicPresenter = musicPresenter;
        _downloadCoordinator = downloadCoordinator;
        _appSettings = appSettings.Value;
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        if (message.Entities?.Any(entity => entity.Type == MessageEntityType.BotCommand) is true)
        {
            await HandleCommandAsync(message, cancellationToken);

            return;
        }

        if (message.Type == MessageType.Text)
            await HandleTextAsync(message, cancellationToken);
    }

    private async Task HandleCommandAsync(Message message, CancellationToken cancellationToken)
    {
        if (message.EntityValues?.Contains("/start") is not true)
            return;

        var replyMarkup = new InlineKeyboardMarkup()
            .AddButton(new InlineKeyboardButton("Search") { SwitchInlineQueryCurrentChat = " " })
            .AddButton(new InlineKeyboardButton("Song") { SwitchInlineQueryCurrentChat = $"{InlineQueryParser.SongKeyword}: " })
            .AddButton(new InlineKeyboardButton("Artist") { SwitchInlineQueryCurrentChat = $"{InlineQueryParser.ArtistKeyword}: " })
            .AddButton(new InlineKeyboardButton("Album") { SwitchInlineQueryCurrentChat = $"{InlineQueryParser.AlbumKeyword}: " })
            .AddButton(new InlineKeyboardButton("Playlist") { SwitchInlineQueryCurrentChat = $"{InlineQueryParser.PlaylistKeyword}: " });

        await _client.SendMessage(
            message.Chat.Id,
            TelegramMessages.Start,
            replyMarkup: replyMarkup,
            cancellationToken: cancellationToken);
    }

    private async Task HandleTextAsync(Message message, CancellationToken cancellationToken)
    {
        var replyParameters = new ReplyParameters { AllowSendingWithoutReply = true, MessageId = message.MessageId };

        if (!Uri.TryCreate(message.Text, UriKind.Absolute, out var targetUrl))
        {
            await _client.SendMessage(
                message.Chat.Id,
                TelegramMessages.WrongUrlFormat,
                replyParameters: replyParameters,
                cancellationToken: cancellationToken);

            return;
        }

        if (!AppleMusicUrl.TryParse(targetUrl, out var reference) || reference is null)
        {
            await _client.SendMessage(
                message.Chat.Id,
                TelegramMessages.WrongAppleMusicUrlFormat,
                replyParameters: replyParameters,
                cancellationToken: cancellationToken);

            return;
        }

        var store = _appSettings.AppleMusic.DefaultStore;

        switch (reference.ResourceType)
        {
            case ResourceType.Songs:
                if (message.From is null)
                    throw new InvalidOperationException("Anonymous downloads are not allowed.");

                await _downloadCoordinator.StartSingleAsync(message.Chat, [reference.Id], store, replyParameters);
                break;

            case ResourceType.Albums:
                await _musicPresenter.ShowAlbumAsync(reference.Id, store, message.Chat, contextMessage: null, replyParameters, cancellationToken);
                break;

            case ResourceType.Artists:
                await _musicPresenter.ShowArtistAsync(reference.Id, store, message.Chat, contextMessage: null, replyParameters, cancellationToken);
                break;

            case ResourceType.Playlists:
                await _musicPresenter.ShowPlaylistAsync(reference.Id, store, message.Chat, contextMessage: null, replyParameters, cancellationToken);
                break;

            default:
                throw new InvalidOperationException("Unknown music element type.");
        }
    }
}
