using System.Text.Json;
using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseTelegramBotWebhook(this IApplicationBuilder applicationBuilder)
    {
        var services = applicationBuilder.ApplicationServices;
        
        applicationBuilder.UseEndpoints(endpoints =>
        {
            endpoints.MapPost("/api/bot/update", async ([FromBody] Update update, IUpdateHandler updateHandler, ILogger<Program> logger) =>
            {
                try
                {
                    await updateHandler.HandleUpdateAsync(update, CancellationToken.None);
                }
                catch (Exception e)
                {
                    logger.LogError(e, message: null);
                }

                return Results.Ok();
            });
        });

        var lifetime = services.GetRequiredService<IHostApplicationLifetime>();
        
        lifetime.ApplicationStarted.Register(() =>
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            var settings = services.GetRequiredService<IOptions<AppSettings>>().Value.Telegram.Webhook!;
            var client = services.GetRequiredService<ITelegramBotClient>();

            logger.LogInformation("Removing webhook");
                
            client.DeleteWebhook().GetAwaiter().GetResult();

            logger.LogInformation($"Setting webhook to {settings.Url}");

            client.SetWebhook(settings.Url!.ToString(),
                    maxConnections: settings.MaxConnections,
                    allowedUpdates: [],
                    dropPendingUpdates: true)
                .GetAwaiter().GetResult();
                
            var webhookInfo = client.GetWebhookInfo().GetAwaiter().GetResult();
                
            logger.LogInformation($"Webhook info: {JsonSerializer.Serialize(webhookInfo)}");
        });

        lifetime.ApplicationStopping.Register(() =>
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            var client = services.GetRequiredService<ITelegramBotClient>();

            client.DeleteWebhook().GetAwaiter().GetResult();
            
            logger.LogInformation("Webhook removed");
        });

        return applicationBuilder;
    }

    public static IApplicationBuilder UseTelegramBotPolling(this IApplicationBuilder applicationBuilder)
    {
        var services = applicationBuilder.ApplicationServices;
        
        var client = services.GetRequiredService<ITelegramBotClient>();

        client.StartReceiving(updateHandler: async (botClient, update, cancellationToken) =>
        {
            await using var scope = services.CreateAsyncScope();
            
            var updateHandler = scope.ServiceProvider.GetRequiredService<IUpdateHandler>();

            await updateHandler.HandleUpdateAsync(update, cancellationToken);
        }, errorHandler: async (botClient, exception, handleErrorsSource, cancellationToken) =>
        {
            await Task.CompletedTask;
        });

        return applicationBuilder;
    }
}