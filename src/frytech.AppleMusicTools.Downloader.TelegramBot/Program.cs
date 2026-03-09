using System.Reflection;
using FluentValidation;
using frytech.AppleMusic.API.Configuration;
using frytech.AppleMusic.API.Extensions;
using frytech.AppleMusicTools.Downloader.Configuration;
using frytech.AppleMusicTools.Downloader.Core;
using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services;
using frytech.AppleMusicTools.Downloader.TelegramBot.Extensions;
using frytech.AppleMusicTools.Widevine.Core.Devices;
using frytech.Essentials.FluentValidation.Tools.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Polly;
using Telegram.Bot;

namespace frytech.AppleMusicTools.Downloader.TelegramBot;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var services = builder.Services;
        var configuration = builder.Configuration;

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), filter: result => !result.ShouldBeSkipped());
        
        services.AddOptions<AppSettings>()
            .Bind(configuration)
            .ValidateFluentValidation()
            .ValidateOnStart();
        
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("App")));
        
        services.AddControllers();

        services.AddHttpClient("Downloader")
            .AddPolicyHandler(GetThresholdPolicy());

        services.AddOptions<AppleMusicClientConfiguration>()
            .PostConfigure((AppleMusicClientConfiguration configuration, IServiceProvider serviceProvider) =>
            {
                var appSettings = serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value;

                configuration.Jwt = appSettings.AppleMusic.ApiToken;
                configuration.BaseUrl = appSettings.AppleMusic.StoreApiUrl;
            });

        services.AddAppleMusicCatalogClient();
        
        services.ConfigureTelegramBot<Microsoft.AspNetCore.Http.Json.JsonOptions>(opt => opt.SerializerOptions);
        
        services.AddSingleton<ITelegramBotClient>(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<AppSettings>>().Value;

            var clientOptions = new TelegramBotClientOptions(settings.Telegram.BotToken, settings.Telegram.ServerApiUrl.ToString());
            
            return new TelegramBotClient(clientOptions)
            {
                Timeout = TimeSpan.FromMinutes(2),
            };
        });
        
        services.AddScoped<IUpdateHandler, UpdateHandler>();
        services.AddScoped<MusicService>();
        services.AddScoped<SongSender>();
        services.AddScoped<SearchResultElementInlineQueryResultProvider>();
        services.AddScoped<ISongCacher, SongCacher>();
        services.AddSingleton<ISongFileProvider, SongFileProvider>();
        
        services.AddSingleton<AppleMusicClient>(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<AppSettings>>().Value;
            
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            
            return new AppleMusicClient(httpClientFactory.CreateClient("Downloader"), new AppleMusicClientOptions()
            {
                ApiToken = settings.AppleMusic.ApiToken,
                MediaToken = settings.AppleMusic.MediaToken,
            });
        });

        services.AddSingleton<WidevineDevice>(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<AppSettings>>().Value;

            var clientIdBytes = File.ReadAllBytes(settings.AppleMusicDownloader.DeviceClientIdFilePath);
            var privateKeyBytes = File.ReadAllBytes(settings.AppleMusicDownloader.DevicePrivateKeyFilePath);
            
            return WidevineDevice.CreateAndroid(clientIdBytes, privateKeyBytes);
        });
        
        services.AddSingleton(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<AppSettings>>().Value;
            
            var client = provider.GetRequiredService<AppleMusicClient>();
            var device = provider.GetRequiredService<WidevineDevice>();

            return new AppleMusicContentDownloader(client, device, new AppleMusicContentDownloaderOptions()
            {
                FfmpegPath = settings.AppleMusicDownloader.FfmpegPath,
                Mp4DecryptPath = settings.AppleMusicDownloader.Mp4DecryptPath,
                Mp4TagPath = settings.AppleMusicDownloader.Mp4TagPath,
            });
        });
        
        var app = builder.Build();
        
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            dbContext.Database.Migrate();
        }
        
        app.UseRouting();
        app.UseAuthorization();
        
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        
        using (var scope = app.Services.CreateScope())
        {
            var settings = scope.ServiceProvider.GetRequiredService<IOptions<AppSettings>>().Value;

            if (settings.Telegram.Webhook?.IsEnabled is true)
                app.UseTelegramBotWebhook();
            else
                app.UseTelegramBotPolling();
        }

        app.MapControllers();
        
        app.Run();
    }
    
    private static IAsyncPolicy<HttpResponseMessage> GetThresholdPolicy()
    {
        return Policy<HttpResponseMessage>
            .HandleResult(_ => true)
            .WaitAndRetryAsync(
                retryCount: 0,
                sleepDurationProvider: _ => TimeSpan.FromSeconds(1),
                onRetry: (_, _, _) => { }
            );
    }
    
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(r => (int)r.StatusCode >= 500)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (response, timespan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} after {timespan.TotalSeconds} seconds due to {response.Exception?.Message ?? response.Result.StatusCode.ToString()}");
                });
    }
}