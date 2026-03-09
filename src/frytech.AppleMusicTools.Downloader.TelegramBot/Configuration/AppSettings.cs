using FluentValidation;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
    
public class AppSettings
{
    [ConfigurationKeyName("TELEGRAM")]
    public required TelegramSection Telegram { get; init; }

    [ConfigurationKeyName("APPLE_MUSIC")]
    public required AppleMusicSection AppleMusic { get; init; }
    
    [ConfigurationKeyName("APPLE_MUSIC_DOWNLOADER")]
    public required AppleMusicDownloaderSection AppleMusicDownloader { get; init; }

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
        
        [ConfigurationKeyName("MEDIA_TOKEN")]
        public required string MediaToken { get; init; }

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
    
    public class AppleMusicDownloaderSection
    {
        [ConfigurationKeyName("FFMPEG_PATH")]
        public required string FfmpegPath { get; init; }
        
        [ConfigurationKeyName("MP4DECRYPT_PATH")]
        public required string Mp4DecryptPath { get; init; }
        
        [ConfigurationKeyName("MP4TAG_PATH")]
        public required string Mp4TagPath { get; init; }
        
        [ConfigurationKeyName("DEVICE_CLIENT_ID_PATH")]
        public required string DeviceClientIdFilePath { get; init; }
        
        [ConfigurationKeyName("DEVICE_PRIVATE_KEY_PATH")]
        public required string DevicePrivateKeyFilePath { get; init; }
    }
    
    public class AppleMusicDownloaderSectionValidator : AbstractValidator<AppleMusicDownloaderSection>
    {
        public AppleMusicDownloaderSectionValidator()
        {
            RuleFor(x => x.FfmpegPath).NotEmpty();
            RuleFor(x => x.Mp4DecryptPath).NotEmpty();
            RuleFor(x => x.Mp4TagPath).NotEmpty();
            RuleFor(x => x.DeviceClientIdFilePath).NotEmpty();
            RuleFor(x => x.DevicePrivateKeyFilePath).NotEmpty();
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
        
        RuleFor(x => x.AppleMusicDownloader)
            .NotEmpty()
            .SetValidator(new AppSettings.AppleMusicDownloaderSectionValidator());

        RuleFor(x => x.Telegram)
            .NotEmpty()
            .SetValidator(new AppSettings.TelegramSectionValidator());
    }
}