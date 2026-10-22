using System.Reflection;
using FluentValidation;
using frytech.AppleMusic.API.Configuration;
using frytech.AppleMusic.API.Extensions;
using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Caching;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Catalog;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Downloads;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Requests;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Requests.Presentation;
using frytech.Essentials.FluentValidation.Tools.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), filter: result => !result.ShouldBeSkipped());

        services.AddOptions<AppSettings>()
            .Bind(configuration)
            .ValidateFluentValidation()
            .ValidateOnStart();

        return services;
    }

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("App")));

        return services;
    }

    public static IServiceCollection AddAppleMusicCatalog(this IServiceCollection services)
    {
        services.AddOptions<AppleMusicClientConfiguration>()
            .PostConfigure((AppleMusicClientConfiguration configuration, IServiceProvider serviceProvider) =>
            {
                var appSettings = serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value;

                configuration.Jwt = appSettings.AppleMusic.ApiToken;
                configuration.BaseUrl = appSettings.AppleMusic.StoreApiUrl;
            });

        services.AddAppleMusicCatalogClient();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services
            .AddScoped<MusicService>()
            .AddScoped<ISongCacher, TelegramStorage>()
            .AddSongFileProviders()
            .AddDownloads()
            .AddRequests();

        return services;
    }

    public static IServiceCollection AddDownloads(this IServiceCollection services)
    {
        services.AddSingleton<SongDownloadCoordinator>();
        services.AddScoped<SongSender>();

        return services;
    }

    public static IServiceCollection AddRequests(this IServiceCollection services)
    {
        services.AddScoped<IUpdateHandler, UpdateHandler>();
        services.AddScoped<InlineQueryHandler>();
        services.AddScoped<CallbackQueryHandler>();
        services.AddScoped<MessageHandler>();
        services.AddScoped<MusicPresenter>();
        services.AddScoped<InlineQueryResultFactory>();

        return services;
    }
}
