using System.Net.Http;
using Telegram.Bot.Exceptions;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Telegram;

public static class TelegramApiRetry
{
    private const int DefaultMaxAttempts = 3;

    public static async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        ILogger logger,
        CancellationToken cancellationToken,
        int maxAttempts = DefaultMaxAttempts)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await action(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (ApiRequestException exception) when (attempt < maxAttempts && IsRetryable(exception))
            {
                var delay = GetDelay(exception, attempt);

                logger.LogWarning(
                    exception,
                    "Telegram API error {ErrorCode}; retrying in {Delay} (attempt {Attempt}/{MaxAttempts})",
                    exception.ErrorCode,
                    delay,
                    attempt,
                    maxAttempts);

                await Task.Delay(delay, cancellationToken);
            }
            catch (HttpRequestException exception) when (attempt < maxAttempts)
            {
                var delay = GetDelay(attempt);

                logger.LogWarning(
                    exception,
                    "Telegram transport error; retrying in {Delay} (attempt {Attempt}/{MaxAttempts})",
                    delay,
                    attempt,
                    maxAttempts);

                await Task.Delay(delay, cancellationToken);
            }
            catch (OperationCanceledException exception) when (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
            {
                var delay = GetDelay(attempt);

                logger.LogWarning(
                    exception,
                    "Telegram request timed out; retrying in {Delay} (attempt {Attempt}/{MaxAttempts})",
                    delay,
                    attempt,
                    maxAttempts);

                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    public static Task ExecuteAsync(
        Func<CancellationToken, Task> action,
        ILogger logger,
        CancellationToken cancellationToken,
        int maxAttempts = DefaultMaxAttempts)
    {
        return ExecuteAsync(
            async requestToken =>
            {
                await action(requestToken);

                return true;
            },
            logger,
            cancellationToken,
            maxAttempts);
    }

    private static bool IsRetryable(ApiRequestException exception)
    {
        return exception.ErrorCode == 429 || exception.ErrorCode >= 500;
    }

    private static TimeSpan GetDelay(ApiRequestException exception, int attempt)
    {
        if (exception.Parameters?.RetryAfter is { } retryAfter)
            return TimeSpan.FromSeconds(retryAfter);

        return GetDelay(attempt);
    }

    private static TimeSpan GetDelay(int attempt)
    {
        return TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
    }
}
