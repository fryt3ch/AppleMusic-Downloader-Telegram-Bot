using FluentValidation;

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
        
        public class WebhookSection
        {
            [ConfigurationKeyName("URL")]
            public Uri? Url { get; init; }

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
        }
    }

    public class DownloaderSection
    {
        [ConfigurationKeyName("PROVIDER")]
        public string Provider { get; init; } = "gamdl";
        
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
        private static readonly string[] AllowedProviders = ["gamdl", "ytdlp"];

        public DownloaderSectionValidator()
        {
            RuleFor(x => x.Provider)
                .NotEmpty()
                .Must(provider => AllowedProviders.Contains(provider, StringComparer.OrdinalIgnoreCase))
                .WithMessage("Provider must be one of: gamdl, ytdlp.");

            RuleFor(x => x.Gamdl).NotNull();
            RuleFor(x => x.YtDlp).NotNull();

            When(x => string.Equals(x.Provider, "gamdl", StringComparison.OrdinalIgnoreCase), () =>
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