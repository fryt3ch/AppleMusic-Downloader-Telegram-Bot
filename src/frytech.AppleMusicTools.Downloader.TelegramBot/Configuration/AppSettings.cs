using FluentValidation;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.SongFiles;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.SongFiles.Providers;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
    
public class AppSettings
{
    [ConfigurationKeyName("TELEGRAM")]
    public required TelegramSection Telegram { get; init; }

    [ConfigurationKeyName("APPLE_MUSIC")]
    public required AppleMusicSection AppleMusic { get; init; }

    [ConfigurationKeyName("DOWNLOADER")]
    public DownloaderSection Downloader { get; init; } = new();

    public class TelegramSection
    {
        [ConfigurationKeyName("BOT_TOKEN")]
        public required string BotToken { get; init; }
        
        [ConfigurationKeyName("SERVER_API_URL")]
        public Uri ServerApiUrl { get; init; } = new Uri("https://api.telegram.org");
        
        [ConfigurationKeyName("WEBHOOK")]
        public WebhookSection? Webhook { get; init; }
        
        [ConfigurationKeyName("STORAGE_CHAT_ID")]
        public required string StorageChatId { get; init; }

        [ConfigurationKeyName("THUMBNAIL_SIZE")]
        public int ThumbnailSize { get; init; } = 1200;

        [ConfigurationKeyName("THUMBNAIL_PREVIEW_SIZE")]
        public int ThumbnailPreviewSize { get; init; } = 120;

        [ConfigurationKeyName("PROGRESS_UPDATE_INTERVAL_SECONDS")]
        public int ProgressUpdateIntervalSeconds { get; init; } = 2;
        
        public class WebhookSection
        {
            [ConfigurationKeyName("URL")]
            public Uri? Url { get; init; }

            [ConfigurationKeyName("PATH")]
            public string Path { get; init; } = "/api/bot/update";

            [ConfigurationKeyName("SECRET_TOKEN")]
            public string? SecretToken { get; init; }

            [ConfigurationKeyName("MAX_CONNECTIONS")]
            public int MaxConnections { get; init; } = 5;
            
            [ConfigurationKeyName("ENABLED")]
            public bool IsEnabled { get; init; } = false;
        }
        
        public class WebhookSectionValidator : AbstractValidator<WebhookSection>
        {
            public WebhookSectionValidator()
            {
                RuleFor(x => x.Url)
                    .NotEmpty()
                    .When(x => x.IsEnabled);

                RuleFor(x => x.Path)
                    .NotEmpty()
                    .Must(path => path.StartsWith('/'))
                    .WithMessage("Webhook path must start with '/'.")
                    .When(x => x.IsEnabled);

                RuleFor(x => x.SecretToken)
                    .MaximumLength(256)
                    .Matches("^[A-Za-z0-9_-]+$")
                    .When(x => !string.IsNullOrEmpty(x.SecretToken));

                RuleFor(x => x.MaxConnections)
                    .InclusiveBetween(1, int.MaxValue);
            }
        }
    }

    public class TelegramSectionValidator : AbstractValidator<TelegramSection>
    {
        public TelegramSectionValidator()
        {
            RuleFor(x => x.StorageChatId).NotEmpty();
            RuleFor(x => x.BotToken).NotEmpty();
            RuleFor(x => x.ServerApiUrl).NotEmpty();
            RuleFor(x => x.ServerApiUrl).Must(uri => uri is { IsAbsoluteUri: true }).WithMessage("Telegram ServerApiUrl must be an absolute URI.");
            RuleFor(x => x.ThumbnailSize).GreaterThan(0);
            RuleFor(x => x.ThumbnailPreviewSize).GreaterThan(0);
            RuleFor(x => x.ProgressUpdateIntervalSeconds).InclusiveBetween(0, 60);
            
            RuleFor(x => x.Webhook!)
                .SetValidator(new TelegramSection.WebhookSectionValidator())
                .When(x => x.Webhook is not null);
        }
    }
    
    public class AppleMusicSection
    {
        [ConfigurationKeyName("API_TOKEN")]
        public required string ApiToken { get; init; }

        [ConfigurationKeyName("DEFAULT_STORE")]
        public required string DefaultStore { get; init; } = "us";

        [ConfigurationKeyName("STORE_API_URL")]
        public required string StoreApiUrl { get; init; } = "https://amp-api-edge.music.apple.com/v1/";
    }
    
    public class AppleMusicSectionValidator : AbstractValidator<AppleMusicSection>
    {
        public AppleMusicSectionValidator()
        {
            RuleFor(x => x.ApiToken).NotEmpty();
            RuleFor(x => x.DefaultStore).NotEmpty();
            RuleFor(x => x.StoreApiUrl).NotEmpty();
            RuleFor(x => x.StoreApiUrl).Must(url => Uri.TryCreate(url, UriKind.Absolute, out _)).WithMessage("Apple Music StoreApiUrl must be an absolute URI.");
        }
    }

    public class DownloaderSection
    {
        [ConfigurationKeyName("PROVIDER")]
        public string Provider { get; init; } = "gamdl";

        [ConfigurationKeyName("PROCESS_TIMEOUT_SECONDS")]
        public int ProcessTimeoutSeconds { get; init; } = 300;
        
        [ConfigurationKeyName("GAMDL")]
        public AppleMusicDownloaderSection Gamdl { get; init; } = new();

        [ConfigurationKeyName("YTDLP")]
        public YtDlpSection YtDlp { get; init; } = new();

        public class AppleMusicDownloaderSection
        {
            [ConfigurationKeyName("COOKIES_PATH")]
            public string? CookiesPath { get; init; }
        }
        
        public class YtDlpSection
        {
            [ConfigurationKeyName("COOKIES_PATH")]
            public string? CookiesPath { get; init; }
        }
    }

    public class DownloaderSectionValidator : AbstractValidator<DownloaderSection>
    {
        private static readonly IReadOnlyList<string> AllowedProviders = SongFileProviderRegistry.Names;

        public DownloaderSectionValidator()
        {
            RuleFor(x => x.Provider)
                .NotEmpty()
                .Must(provider => AllowedProviders.Contains(provider, StringComparer.OrdinalIgnoreCase))
                .WithMessage(_ => $"Provider must be one of: {string.Join(", ", AllowedProviders)}.");

            RuleFor(x => x.Gamdl).NotNull();
            RuleFor(x => x.YtDlp).NotNull();
            RuleFor(x => x.ProcessTimeoutSeconds).InclusiveBetween(1, 86400);

            When(x => string.Equals(x.Provider, GamdlSongFileProvider.Name, StringComparison.OrdinalIgnoreCase), () =>
            {
                RuleFor(x => x.Gamdl.CookiesPath)
                    .NotEmpty()
                    .WithMessage("DOWNLOADER__GAMDL__COOKIES_PATH is required when PROVIDER=gamdl.");
            });
        }
    }
}

public class AppSettingsValidator : AbstractValidator<AppSettings>
{
    public AppSettingsValidator()
    {
        RuleFor(x => x.AppleMusic)
            .NotEmpty()
            .SetValidator(new AppSettings.AppleMusicSectionValidator());

        RuleFor(x => x.Downloader)
            .NotEmpty()
            .SetValidator(new AppSettings.DownloaderSectionValidator());

        RuleFor(x => x.Telegram)
            .NotEmpty()
            .SetValidator(new AppSettings.TelegramSectionValidator());
    }
}