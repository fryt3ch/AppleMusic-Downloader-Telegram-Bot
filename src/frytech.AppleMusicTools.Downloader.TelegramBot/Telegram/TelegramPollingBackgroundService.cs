using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Requests;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Telegram;

public sealed class TelegramPollingBackgroundService : BackgroundService
{
    private readonly ITelegramBotClient _client;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppSettings _settings;
    private readonly ILogger<TelegramPollingBackgroundService> _logger;

    public TelegramPollingBackgroundService(
        ITelegramBotClient client,
        IServiceScopeFactory scopeFactory,
        IOptions<AppSettings> appSettings,
        ILogger<TelegramPollingBackgroundService> logger)
    {
        _client = client;
        _scopeFactory = scopeFactory;
        _settings = appSettings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_settings.Telegram.Webhook?.IsEnabled is true)
            return;

        _client.StartReceiving(
            async (_, update, cancellationToken) =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                var updateHandler = scope.ServiceProvider.GetRequiredService<IUpdateHandler>();

                await updateHandler.HandleUpdateAsync(update, cancellationToken);
            },
            (_, ex, source, _) =>
            {
                _logger.LogError(ex, "Telegram polling error ({Source})", source);

                return Task.CompletedTask;
            },
            cancellationToken: stoppingToken);

        _logger.LogInformation("Telegram polling started");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Telegram polling stopped");
        }
    }
}
