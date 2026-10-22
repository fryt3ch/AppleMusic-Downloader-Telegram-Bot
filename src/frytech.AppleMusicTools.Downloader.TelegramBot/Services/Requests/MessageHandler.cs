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
    private readonly ILogger<MessageHandler> _logger;

    public MessageHandler(
        ITelegramBotClient client,
        MusicPresenter musicPresenter,
        SongDownloadCoordinator downloadCoordinator,
        IOptions<AppSettings> appSettings,
        ILogger<MessageHandler> logger)
    {
        _client = client;
        _musicPresenter = musicPresenter;
        _downloadCoordinator = downloadCoordinator;
        _appSettings = appSettings.Value;
        _logger = logger;
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        if (message.Entities?.Any(entity => entity.Type == MessageEntityType.BotCommand) is true)
        {
            _logger.LogDebug("Handling command message {MessageId} in chat {ChatId}", message.MessageId, message.Chat.Id);

            await HandleCommandAsync(message, cancellationToken);

            return;
        }

        if (message.Type == MessageType.Text)
        {
            _logger.LogDebug("Handling text message {MessageId} in chat {ChatId}", message.MessageId, message.Chat.Id);

            await HandleTextAsync(message, cancellationToken);
        }
    }

    private async Task HandleCommandAsync(Message message, CancellationToken cancellationToken)
    {
        if (message.EntityValues?.Contains("/start") is not true)
            return;

        _logger.LogInformation("Received /start command in chat {ChatId}", message.Chat.Id);

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
            _logger.LogDebug("Received non-URL text from chat {ChatId}", message.Chat.Id);

            await _client.SendMessage(
                message.Chat.Id,
                TelegramMessages.WrongUrlFormat,
                replyParameters: replyParameters,
                cancellationToken: cancellationToken);

            return;
        }

        if (!AppleMusicUrl.TryParse(targetUrl, out var reference) || reference is null)
        {
            _logger.LogDebug("Received unsupported URL {Url} from chat {ChatId}", targetUrl, message.Chat.Id);

            await _client.SendMessage(
                message.Chat.Id,
                TelegramMessages.WrongAppleMusicUrlFormat,
                replyParameters: replyParameters,
                cancellationToken: cancellationToken);

            return;
        }

        _logger.LogDebug("Parsed Apple Music URL {Url} as {ResourceType} {ResourceId}", targetUrl, reference.ResourceType, reference.Id);

        _logger.LogInformation("Resolved Apple Music {ResourceType} {ResourceId} from URL in chat {ChatId}", reference.ResourceType, reference.Id, message.Chat.Id);

        var store = _appSettings.AppleMusic.DefaultStore;

        try
        {
            switch (reference.ResourceType)
            {
                case ResourceType.Songs:
                    if (message.From is null)
                    {
                        _logger.LogWarning("Ignoring anonymous song download request in chat {ChatId}", message.Chat.Id);

                        return;
                    }

                    await _downloadCoordinator.StartSingleAsync(message.Chat, message.From, reference.Id, store, replyParameters, cancellationToken);
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
                    _logger.LogWarning("Unsupported music element type {ResourceType}", reference.ResourceType);
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle Apple Music URL {Url} in chat {ChatId}", targetUrl, message.Chat.Id);

            await _client.SendMessage(
                message.Chat.Id,
                "Couldn't load this item. Please try again later.",
                replyParameters: replyParameters,
                cancellationToken: cancellationToken);
        }
    }
}
