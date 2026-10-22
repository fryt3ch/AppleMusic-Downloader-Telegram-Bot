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

    public Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Handling Telegram update {UpdateType} {UpdateId}", update.Type, update.Id);

        return update.Type switch
        {
            UpdateType.InlineQuery when update.InlineQuery is { } inlineQuery =>
                _inlineQueryHandler.HandleAsync(inlineQuery, cancellationToken),

            UpdateType.CallbackQuery when update.CallbackQuery is { } callbackQuery =>
                _callbackQueryHandler.HandleAsync(callbackQuery, cancellationToken),

            UpdateType.Message when update.Message is { } message =>
                _messageHandler.HandleAsync(message, cancellationToken),

            _ => Task.CompletedTask,
        };
    }
}
