using System.Diagnostics;
using frytech.AppleMusicTools.Downloader.TelegramBot.Resources;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Downloads;

public sealed class DownloadProgressReporter
{
    private readonly ITelegramBotClient _client;
    private readonly Chat _chat;
    private readonly ReplyParameters? _replyParameters;
    private readonly InlineKeyboardMarkup? _replyMarkup;
    private readonly Action<int?>? _statusMessageIdChanged;
    private readonly TimeSpan _updateInterval;
    private readonly ILogger _logger;

    private int? _messageId;
    private string? _lastText;
    private long _lastUpdateTimestamp;

    public DownloadProgressReporter(
        ITelegramBotClient client,
        Chat chat,
        ReplyParameters? replyParameters,
        int? statusMessageId,
        InlineKeyboardMarkup? replyMarkup,
        Action<int?>? statusMessageIdChanged,
        TimeSpan updateInterval,
        ILogger logger)
    {
        _client = client;
        _chat = chat;
        _replyParameters = replyParameters;
        _replyMarkup = replyMarkup;
        _statusMessageIdChanged = statusMessageIdChanged;
        _updateInterval = updateInterval;
        _logger = logger;
        _messageId = statusMessageId;
    }

    public Task StartAsync(int total, int failed, CancellationToken cancellationToken)
    {
        return SendOrEditAsync(FormatGathering(0, total, failed), cancellationToken);
    }

    public async Task ReportAsync(int processed, int total, int failed, CancellationToken cancellationToken)
    {
        if (!ShouldUpdate())
            return;

        await SendOrEditAsync(FormatGathering(processed, total, failed), cancellationToken);
    }

    public Task ReportSendingAsync(int sent, int total, CancellationToken cancellationToken)
    {
        var text = string.Format(TelegramMessages.ProgressSending, sent, total, TelegramMessages.ProgressWarning);

        return SendOrEditAsync(text, cancellationToken);
    }

    public async Task MoveToBottomAsync(CancellationToken cancellationToken)
    {
        var messageId = _messageId;

        if (messageId is null)
            return;

        var text = _lastText;

        try
        {
            await _client.DeleteMessage(_chat.Id, messageId.Value, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to move the download progress message in chat {ChatId}", _chat.Id);
        }

        _messageId = null;
        _lastText = null;
        _statusMessageIdChanged?.Invoke(null);

        if (text is not null)
            await SendOrEditAsync(text, cancellationToken);
    }

    public async Task CompleteAsync(bool deleteMessage = true)
    {
        var messageId = _messageId;

        if (messageId is null || !deleteMessage)
            return;

        _messageId = null;
        _lastText = null;

        try
        {
            await _client.DeleteMessage(_chat.Id, messageId.Value, CancellationToken.None);
            _statusMessageIdChanged?.Invoke(null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete the download progress message in chat {ChatId}", _chat.Id);
        }
    }

    private static string FormatGathering(int processed, int total, int failed)
    {
        if (total <= 1)
            return TelegramMessages.DownloadingStatus;

        var failedSuffix = failed > 0
            ? $" {string.Format(TelegramMessages.ProgressFailedSuffix, failed)}"
            : string.Empty;

        return string.Format(TelegramMessages.ProgressGathering, processed, total, failedSuffix, TelegramMessages.ProgressWarning);
    }

    private bool ShouldUpdate()
    {
        if (_updateInterval <= TimeSpan.Zero)
            return true;

        return Stopwatch.GetElapsedTime(_lastUpdateTimestamp) >= _updateInterval;
    }

    private async Task SendOrEditAsync(string text, CancellationToken cancellationToken, bool allowRetry = true)
    {
        if (string.Equals(text, _lastText, StringComparison.Ordinal))
            return;

        try
        {
            if (_messageId is { } messageId)
            {
                await _client.EditMessageText(
                    _chat.Id,
                    messageId,
                    text,
                    replyMarkup: _replyMarkup,
                    cancellationToken: cancellationToken);
            }
            else
            {
                var message = await _client.SendMessage(
                    _chat.Id,
                    text,
                    replyParameters: _replyParameters,
                    disableNotification: true,
                    replyMarkup: _replyMarkup,
                    cancellationToken: cancellationToken);

                _messageId = message.MessageId;
                _statusMessageIdChanged?.Invoke(message.MessageId);
            }

            _lastText = text;
            _lastUpdateTimestamp = Stopwatch.GetTimestamp();
        }
        catch (ApiRequestException exception) when (allowRetry && exception.Parameters?.RetryAfter is { } retryAfter)
        {
            await Task.Delay(TimeSpan.FromSeconds(retryAfter), cancellationToken);

            await SendOrEditAsync(text, cancellationToken, allowRetry: false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _lastUpdateTimestamp = Stopwatch.GetTimestamp();

            _logger.LogWarning(ex, "Failed to update the download progress message in chat {ChatId}", _chat.Id);
        }
    }
}
