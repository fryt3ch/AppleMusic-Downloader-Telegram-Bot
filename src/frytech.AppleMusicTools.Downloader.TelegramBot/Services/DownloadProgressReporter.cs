using System.Diagnostics;
using frytech.AppleMusicTools.Downloader.TelegramBot.Resources;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services;

internal sealed class DownloadProgressReporter
{
    private readonly ITelegramBotClient _client;
    private readonly Chat _chat;
    private readonly ReplyParameters? _replyParameters;
    private readonly TimeSpan _updateInterval;
    private readonly ILogger _logger;

    private Message? _statusMessage;
    private string? _lastText;
    private long _lastUpdateTimestamp;

    public DownloadProgressReporter(
        ITelegramBotClient client,
        Chat chat,
        ReplyParameters? replyParameters,
        TimeSpan updateInterval,
        ILogger logger)
    {
        _client = client;
        _chat = chat;
        _replyParameters = replyParameters;
        _updateInterval = updateInterval;
        _logger = logger;
    }

    public Task StartAsync(int total, CancellationToken cancellationToken)
    {
        return SendOrEditAsync(FormatGathering(0, total), cancellationToken);
    }

    public async Task ReportAsync(int processed, int total, CancellationToken cancellationToken)
    {
        if (!ShouldUpdate())
            return;

        await SendOrEditAsync(FormatGathering(processed, total), cancellationToken);
    }

    public Task ReportSendingAsync(int sent, int total, CancellationToken cancellationToken)
    {
        var text = string.Format(TelegramMessages.ProgressSending, sent, total, TelegramMessages.ProgressWarning);

        return SendOrEditAsync(text, cancellationToken);
    }

    public async Task CompleteAsync()
    {
        var statusMessage = _statusMessage;

        if (statusMessage is null)
            return;

        _statusMessage = null;
        _lastText = null;

        try
        {
            await _client.DeleteMessage(_chat.Id, statusMessage.MessageId, CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to delete the download progress message in chat {ChatId}", _chat.Id);
        }
    }

    private static string FormatGathering(int processed, int total)
    {
        return string.Format(TelegramMessages.ProgressGathering, processed, total, TelegramMessages.ProgressWarning);
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
            var statusMessage = _statusMessage;

            var message = statusMessage is null
                ? await _client.SendMessage(
                    _chat.Id,
                    text,
                    replyParameters: _replyParameters,
                    disableNotification: true,
                    cancellationToken: cancellationToken)
                : await _client.EditMessageText(
                    _chat.Id,
                    statusMessage.MessageId,
                    text,
                    cancellationToken: cancellationToken);

            _statusMessage = message;
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
        catch (Exception exception)
        {
            _lastUpdateTimestamp = Stopwatch.GetTimestamp();

            _logger.LogWarning(exception, "Failed to update the download progress message in chat {ChatId}", _chat.Id);
        }
    }
}
