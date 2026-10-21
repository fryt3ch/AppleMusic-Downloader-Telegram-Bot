using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Telegram;

public static class TelegramBotServiceCollectionExtensions
{
    public static IServiceCollection AddTelegramBot(this IServiceCollection services)
    {
        services.ConfigureTelegramBot<JsonOptions>(options => options.SerializerOptions);

        services.AddSingleton<ITelegramBotClient>(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<AppSettings>>().Value;

            var clientOptions = new TelegramBotClientOptions(
                settings.Telegram.BotToken,
                settings.Telegram.ServerApiUrl.ToString());

            return new TelegramBotClient(clientOptions)
            {
                Timeout = TimeSpan.FromMinutes(2),
            };
        });

        services.AddHostedService<TelegramWebhookInitializer>();
        services.AddHostedService<TelegramPollingBackgroundService>();

        return services;
    }
}
