using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.SongFiles;
using frytech.AppleMusicTools.Downloader.TelegramBot.Telegram;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Extensions;

public static class WebApplicationExtensions
{
    public static async Task InitializeAsync(this WebApplication app)
    {
        var logger = app.Logger;
        var settings = app.Services.GetRequiredService<IOptions<AppSettings>>().Value;

        logger.LogInformation("Applying database migrations...");

        try
        {
            await using var scope = app.Services.CreateAsyncScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.Database.MigrateAsync();

            logger.LogInformation("Database migrations applied");
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "Database migration failed");

            throw;
        }

        logger.LogInformation("Song file providers: {Providers}", string.Join(", ", SongFileProviderRegistry.Names));
        logger.LogInformation("Active download provider: {Provider}", settings.Downloader.Provider);
        logger.LogInformation("Apple Music store: {Store}", settings.AppleMusic.DefaultStore);
        logger.LogInformation("Telegram mode: {Mode}", settings.Telegram.Webhook?.IsEnabled is true ? "webhook" : "polling");
    }

    public static WebApplication UseTelegramBot(this WebApplication app)
    {
        var settings = app.Services.GetRequiredService<IOptions<AppSettings>>().Value;

        if (settings.Telegram.Webhook is { IsEnabled: true } webhook)
            app.MapTelegramWebhook(webhook);

        return app;
    }
}
