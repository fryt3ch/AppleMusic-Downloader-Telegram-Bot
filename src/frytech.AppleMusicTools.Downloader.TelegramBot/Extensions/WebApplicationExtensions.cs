using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services;
using frytech.AppleMusicTools.Downloader.TelegramBot.Telegram;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Extensions;

public static class WebApplicationExtensions
{
    public static async Task InitializeAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.MigrateAsync();
    }

    public static WebApplication UseTelegramBot(this WebApplication app)
    {
        var settings = app.Services.GetRequiredService<IOptions<AppSettings>>().Value;

        if (settings.Telegram.Webhook is { IsEnabled: true } webhook)
            app.MapTelegramWebhook(webhook);

        return app;
    }
}
