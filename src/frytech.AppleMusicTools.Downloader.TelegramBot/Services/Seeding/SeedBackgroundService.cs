using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using Microsoft.Extensions.Options;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Seeding;

public sealed class SeedBackgroundService : BackgroundService
{
    private readonly SeedRunner _runner;
    private readonly AppSettings _appSettings;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<SeedBackgroundService> _logger;

    public SeedBackgroundService(SeedRunner runner, IOptions<AppSettings> appSettings, IHostApplicationLifetime lifetime, ILogger<SeedBackgroundService> logger)
    {
        _runner = runner;
        _appSettings = appSettings.Value;
        _lifetime = lifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_appSettings.Seed.Enabled)
            return;

        _logger.LogInformation("Seed background service is enabled");

        var ran = await _runner.RunAsync(stoppingToken);

        if (ran && _appSettings.Seed.ExitWhenDone && !stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Seed finished and SEED__EXIT_WHEN_DONE=true; stopping the application");

            _lifetime.StopApplication();
        }
    }
}
