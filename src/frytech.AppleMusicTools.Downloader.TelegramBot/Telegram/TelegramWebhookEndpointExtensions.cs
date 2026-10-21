using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services;
using Telegram.Bot.Types;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Telegram;

public static class TelegramWebhookEndpointExtensions
{
    private const string SecretTokenHeader = "X-Telegram-Bot-Api-Secret-Token";

    public static IEndpointRouteBuilder MapTelegramWebhook(
        this IEndpointRouteBuilder endpoints,
        AppSettings.TelegramSection.WebhookSection webhook)
    {
        endpoints.MapPost(webhook.Path, async (
            Update update,
            HttpContext httpContext,
            IUpdateHandler updateHandler,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            if (!string.IsNullOrEmpty(webhook.SecretToken) &&
                !string.Equals(
                    httpContext.Request.Headers[SecretTokenHeader].ToString(),
                    webhook.SecretToken,
                    StringComparison.Ordinal))
            {
                return Results.Unauthorized();
            }

            var logger = loggerFactory.CreateLogger("Telegram.Webhook");

            try
            {
                await updateHandler.HandleUpdateAsync(update, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to handle Telegram update {UpdateId}", update.Id);
            }

            return Results.Ok();
        });

        return endpoints;
    }
}
