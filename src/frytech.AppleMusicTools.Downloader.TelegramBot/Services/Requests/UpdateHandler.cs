using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Requests;

public sealed class UpdateHandler : IUpdateHandler
{
    private readonly InlineQueryHandler _inlineQueryHandler;
    private readonly CallbackQueryHandler _callbackQueryHandler;
    private readonly MessageHandler _messageHandler;
    private readonly ILogger<UpdateHandler> _logger;

    public UpdateHandler(
        InlineQueryHandler inlineQueryHandler,
        CallbackQueryHandler callbackQueryHandler,
        MessageHandler messageHandler,
        ILogger<UpdateHandler> logger)
    {
        _inlineQueryHandler = inlineQueryHandler;
        _callbackQueryHandler = callbackQueryHandler;
        _messageHandler = messageHandler;
        _logger = logger;
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        var user = GetUser(update);
        var chat = GetChat(update);

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["UpdateId"] = update.Id,
            ["UpdateType"] = update.Type.ToString(),
            ["UserId"] = user?.Id,
            ["Username"] = user?.Username,
            ["ChatId"] = chat?.Id,
        });

        _logger.LogDebug("Handling update {UpdateId}", update.Id);

        try
        {
            switch (update.Type)
            {
                case UpdateType.InlineQuery when update.InlineQuery is { } inlineQuery:
                    await _inlineQueryHandler.HandleAsync(inlineQuery, cancellationToken);
                    break;

                case UpdateType.CallbackQuery when update.CallbackQuery is { } callbackQuery:
                    await _callbackQueryHandler.HandleAsync(callbackQuery, cancellationToken);
                    break;

                case UpdateType.Message when update.Message is { } message:
                    await _messageHandler.HandleAsync(message, cancellationToken);
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to handle update {UpdateId}", update.Id);

            throw;
        }
    }

    private static User? GetUser(Update update)
    {
        return update.Message?.From ?? update.CallbackQuery?.From ?? update.InlineQuery?.From;
    }

    private static Chat? GetChat(Update update)
    {
        return update.Message?.Chat ?? update.CallbackQuery?.Message?.Chat;
    }
}
