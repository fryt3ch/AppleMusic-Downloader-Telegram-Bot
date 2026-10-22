using System.Text.Json;
using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Telegram;

public sealed class TelegramWebhookInitializer : IHostedService
{
    private readonly ITelegramBotClient _client;
    private readonly AppSettings _settings;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<TelegramWebhookInitializer> _logger;

    public TelegramWebhookInitializer(
        ITelegramBotClient client,
        IOptions<AppSettings> appSettings,
        IHostApplicationLifetime lifetime,
        ILogger<TelegramWebhookInitializer> logger)
    {
        _client = client;
        _settings = appSettings.Value;
        _lifetime = lifetime;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_settings.Telegram.Webhook?.IsEnabled is not true)
            return Task.CompletedTask;

        _lifetime.ApplicationStarted.Register(() => _ = SetWebhookAsync());

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_settings.Telegram.Webhook?.IsEnabled is not true)
            return;

        try
        {
            await _client.DeleteWebhook(cancellationToken: cancellationToken);

            _logger.LogInformation("Telegram webhook removed");
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to remove Telegram webhook");
        }
    }

    private async Task SetWebhookAsync()
    {
        var webhook = _settings.Telegram.Webhook!;

        try
        {
            _logger.LogInformation("Removing Telegram webhook");
            
            await _client.DeleteWebhook(cancellationToken: CancellationToken.None);

            _logger.LogInformation("Setting Telegram webhook to {Url}", webhook.Url);
            
            await _client.SetWebhook(
                webhook.Url!.ToString(),
                maxConnections: webhook.MaxConnections,
                allowedUpdates: [],
                dropPendingUpdates: true,
                secretToken: webhook.SecretToken,
                cancellationToken: CancellationToken.None);

            var webhookInfo = await _client.GetWebhookInfo(cancellationToken: CancellationToken.None);

            _logger.LogInformation("Telegram webhook info: {WebhookInfo}", JsonSerializer.Serialize(webhookInfo));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to configure Telegram webhook");
        }
    }
}
