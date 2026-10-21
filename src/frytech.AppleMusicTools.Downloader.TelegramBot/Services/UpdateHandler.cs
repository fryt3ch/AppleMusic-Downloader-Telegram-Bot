using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services;

public sealed class UpdateHandler : IUpdateHandler
{
    private readonly InlineQueryHandler _inlineQueryHandler;
    private readonly CallbackQueryHandler _callbackQueryHandler;
    private readonly MessageHandler _messageHandler;

    public UpdateHandler(
        InlineQueryHandler inlineQueryHandler,
        CallbackQueryHandler callbackQueryHandler,
        MessageHandler messageHandler)
    {
        _inlineQueryHandler = inlineQueryHandler;
        _callbackQueryHandler = callbackQueryHandler;
        _messageHandler = messageHandler;
    }

    public Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
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
