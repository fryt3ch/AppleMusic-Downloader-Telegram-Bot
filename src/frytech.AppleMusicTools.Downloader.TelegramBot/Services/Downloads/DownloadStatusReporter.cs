using frytech.AppleMusicTools.Downloader.TelegramBot.Models.Downloads;
using frytech.AppleMusicTools.Downloader.TelegramBot.Telegram;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Downloads;

public sealed class DownloadStatusReporter
{
    private readonly ITelegramBotClient _client;
    private readonly ILogger<DownloadStatusReporter> _logger;

    public DownloadStatusReporter(ITelegramBotClient client, ILogger<DownloadStatusReporter> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<int?> SendStatusAsync(DownloadRequest request, string text, InlineKeyboardMarkup replyMarkup, int? existingMessageId, CancellationToken cancellationToken)
    {
        if (existingMessageId is { } messageId)
        {
            try
            {
                await TelegramApiRetry.ExecuteAsync(
                    requestToken => _client.EditMessageText(
                        request.Chat.Id,
                        messageId,
                        text,
                        replyMarkup: replyMarkup,
                        cancellationToken: requestToken),
                    _logger,
                    cancellationToken);

                return messageId;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to reuse status message {MessageId} for {DownloadId}", messageId, request.DownloadId);
            }
        }

        try
        {
            var message = await TelegramApiRetry.ExecuteAsync(
                requestToken => _client.SendMessage(
                    request.Chat.Id,
                    text,
                    replyParameters: request.ReplyParameters,
                    disableNotification: true,
                    replyMarkup: replyMarkup,
                    cancellationToken: requestToken),
                _logger,
                cancellationToken);

            return message.MessageId;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send download status message for {DownloadId}", request.DownloadId);
            return null;
        }
    }

    public async Task SendBatchNoticeAsync(Chat chat, DownloadBatchNotice notice, CancellationToken cancellationToken)
    {
        if (notice.ReplaceMessageId is { } messageId)
        {
            try
            {
                await _client.DeleteMessage(chat.Id, messageId, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ApiRequestException ex) when (ex.ErrorCode == 400)
            {
                _logger.LogDebug(ex, "Previous batch notice {MessageId} was already removed in chat {ChatId}", messageId, chat.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete the previous batch notice {MessageId} in chat {ChatId}", messageId, chat.Id);
            }
        }

        try
        {
            await TelegramApiRetry.ExecuteAsync(
                requestToken => _client.SendMessage(
                    chat.Id,
                    notice.Text,
                    replyParameters: notice.ReplyParameters,
                    disableNotification: true,
                    replyMarkup: notice.ReplyMarkup,
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
            _logger.LogWarning(ex, "Failed to send the batch notice in chat {ChatId}", chat.Id);
        }
    }

    public async Task RestoreBatchMessageAsync(DownloadRequest request, DownloadBatchMessage message, CancellationToken cancellationToken)
    {
        if (request.StatusMessageId is not { } messageId)
            return;

        try
        {
            await TelegramApiRetry.ExecuteAsync(
                requestToken => _client.EditMessageText(
                    request.Chat.Id,
                    messageId,
                    message.Text,
                    replyMarkup: message.ReplyMarkup,
                    cancellationToken: requestToken),
                _logger,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ApiRequestException ex) when (ex.ErrorCode == 400)
        {
            _logger.LogDebug(ex, "Failed to restore batch message {MessageId} for {DownloadId}", messageId, request.DownloadId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to restore batch message {MessageId} for {DownloadId}", messageId, request.DownloadId);
        }
    }

    public async Task EditStatusAsync(DownloadRequest request, string text, InlineKeyboardMarkup? replyMarkup, CancellationToken cancellationToken)
    {
        if (request.StatusMessageId is not { } messageId)
            return;

        try
        {
            await TelegramApiRetry.ExecuteAsync(
                requestToken => _client.EditMessageText(
                    request.Chat.Id,
                    messageId,
                    text,
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
            _logger.LogWarning(ex, "Failed to edit download status message for {DownloadId}", request.DownloadId);
        }
    }

    public Task FinalizeAsync(DownloadRequest request, string text, CancellationToken cancellationToken)
    {
        return EditStatusAsync(request, text, new InlineKeyboardMarkup(), cancellationToken);
    }

    public async Task DeleteAsync(DownloadRequest request, CancellationToken cancellationToken)
    {
        if (request.StatusMessageId is not { } messageId)
            return;

        try
        {
            await _client.DeleteMessage(request.Chat.Id, messageId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete download status message for {DownloadId}", request.DownloadId);
        }
    }
}
