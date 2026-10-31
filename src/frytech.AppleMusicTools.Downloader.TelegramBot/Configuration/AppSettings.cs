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

    [ConfigurationKeyName("SEED")]
    public SeedSection Seed { get; init; } = new();

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
        
        [ConfigurationKeyName("DOWNLOADS")]
        public DownloadsSection Downloads { get; init; } = new();
        
        public class WebhookSection
        {
            [ConfigurationKeyName("URL")]
            public Uri? Url { get; init; }

            [ConfigurationKeyName("PATH")]
            public string Path { get; init; } = "/api/bot/update";

            [ConfigurationKeyName("SECRET_TOKEN")]
            public string? SecretToken { get; init; }

            [ConfigurationKeyName("MAX_CONNECTIONS")]
            public int MaxConnections { get; init; } = 50;
            
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
        
        public class DownloadsSection
        {
            [ConfigurationKeyName("MAX_CONCURRENCY")]
            public int MaxConcurrency { get; init; } = 2;

            [ConfigurationKeyName("MAX_PENDING_SONGS")]
            public int MaxPendingSongs { get; init; } = 2000;

            [ConfigurationKeyName("MAX_PENDING_BULK_PER_USER")]
            public int MaxPendingBulkPerUser { get; init; } = 1;

            [ConfigurationKeyName("MAX_SONGS_PER_BATCH")]
            public int MaxSongsPerBatch { get; init; } = 250;

            [ConfigurationKeyName("SONGS_PAGE_SIZE")]
            public int SongsPageSize { get; init; } = 250;
        }

        public class DownloadsSectionValidator : AbstractValidator<DownloadsSection>
        {
            public DownloadsSectionValidator()
            {
                RuleFor(x => x.MaxConcurrency).InclusiveBetween(1, 16);
                RuleFor(x => x.MaxPendingSongs).InclusiveBetween(1, 100000);
                RuleFor(x => x.MaxPendingBulkPerUser).InclusiveBetween(1, 10);
                RuleFor(x => x.MaxSongsPerBatch)
                    .InclusiveBetween(1, 5000)
                    .WithMessage("MAX_SONGS_PER_BATCH must be between 1 and 5000.");
                RuleFor(x => x.SongsPageSize)
                    .InclusiveBetween(1, 250)
                    .WithMessage("SONGS_PAGE_SIZE must be between 1 and 250 because Apple Music returns at most 250 items per page.");
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
            
            RuleFor(x => x.Downloads)
                .NotNull()
                .SetValidator(new TelegramSection.DownloadsSectionValidator());
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

        [ConfigurationKeyName("MAX_CONCURRENCY")]
        public int MaxConcurrency { get; init; } = 3;

        [ConfigurationKeyName("DELAY_AFTER_DOWNLOAD_SECONDS")]
        public int DelayAfterDownloadSeconds { get; init; } = 3;
        
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

    public class SeedSection
    {
        [ConfigurationKeyName("ENABLED")]
        public bool Enabled { get; init; }

        [ConfigurationKeyName("PROVIDER")]
        public string? Provider { get; init; }

        [ConfigurationKeyName("ARTISTS_FILE")]
        public string ArtistsFile { get; init; } = "data/artists.txt";

        [ConfigurationKeyName("START_INDEX")]
        public int StartIndex { get; init; }

        [ConfigurationKeyName("MAX_ARTISTS")]
        public int MaxArtists { get; init; } = 50;

        [ConfigurationKeyName("MAX_SONGS")]
        public int MaxSongs { get; init; }

        [ConfigurationKeyName("MAX_SONGS_PER_ARTIST")]
        public int MaxSongsPerArtist { get; init; }

        [ConfigurationKeyName("CONCURRENCY")]
        public int Concurrency { get; init; } = 8;

        [ConfigurationKeyName("CATALOG_CONCURRENCY")]
        public int CatalogConcurrency { get; init; } = 4;

        [ConfigurationKeyName("MAX_ATTEMPTS")]
        public int MaxAttempts { get; init; } = 3;

        [ConfigurationKeyName("MAX_CONSECUTIVE_FAILURES")]
        public int MaxConsecutiveFailures { get; init; } = 20;

        [ConfigurationKeyName("STATE_PATH")]
        public string StatePath { get; init; } = "data/seed-state.json";

        [ConfigurationKeyName("DRY_RUN")]
        public bool DryRun { get; init; }

        [ConfigurationKeyName("EXIT_WHEN_DONE")]
        public bool ExitWhenDone { get; init; }

        [ConfigurationKeyName("INCLUDE_SINGLES")]
        public bool IncludeSingles { get; init; } = true;

        [ConfigurationKeyName("MIN_TRACK_SECONDS")]
        public int MinTrackSeconds { get; init; } = 30;

        [ConfigurationKeyName("EXCLUDE_ALBUM_PATTERNS")]
        public string ExcludeAlbumPatterns { get; init; } =
            "live,remix,deluxe,super deluxe,anniversary,expanded,compilation,greatest hits,best of,essential,anthology,collection,box set,demo,rehearsal,session,instrumental,karaoke,commentary,tour";

        [ConfigurationKeyName("EXCLUDE_TRACK_PATTERNS")]
        public string ExcludeTrackPatterns { get; init; } =
            "live,remix,instrumental,karaoke,commentary,demo,rehearsal,take";

        [ConfigurationKeyName("ARTIST_MATCH_THRESHOLD")]
        public double ArtistMatchThreshold { get; init; } = 0.82;

        [ConfigurationKeyName("FALLBACK_ARTIST_SONGS")]
        public bool FallbackArtistSongs { get; init; } = true;

        [ConfigurationKeyName("FALLBACK_ARTIST_SONGS_LIMIT")]
        public int FallbackArtistSongsLimit { get; init; } = 50;

        public class SeedSectionValidator : AbstractValidator<SeedSection>
        {
            public SeedSectionValidator()
            {
                RuleFor(x => x.Provider)
                    .Must(provider => string.IsNullOrWhiteSpace(provider) ||
                                      SongFileProviderRegistry.Names.Contains(provider, StringComparer.OrdinalIgnoreCase))
                    .WithMessage(_ => $"Seed provider must be empty or one of: {string.Join(", ", SongFileProviderRegistry.Names)}.");

                RuleFor(x => x.ArtistsFile).NotEmpty();
                RuleFor(x => x.StatePath).NotEmpty();
                RuleFor(x => x.StartIndex).GreaterThanOrEqualTo(0);
                RuleFor(x => x.MaxArtists).GreaterThanOrEqualTo(0);
                RuleFor(x => x.MaxSongs).GreaterThanOrEqualTo(0);
                RuleFor(x => x.MaxSongsPerArtist).GreaterThanOrEqualTo(0);
                RuleFor(x => x.Concurrency).InclusiveBetween(1, 32);
                RuleFor(x => x.CatalogConcurrency).InclusiveBetween(1, 16);
                RuleFor(x => x.MaxAttempts).InclusiveBetween(1, 10);
                RuleFor(x => x.MaxConsecutiveFailures).InclusiveBetween(1, 1000);
                RuleFor(x => x.MinTrackSeconds).InclusiveBetween(0, 3600);
                RuleFor(x => x.ArtistMatchThreshold).InclusiveBetween(0.5, 1.0);
                RuleFor(x => x.FallbackArtistSongsLimit).InclusiveBetween(0, 1000);
            }
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
            RuleFor(x => x.MaxConcurrency).InclusiveBetween(1, 16);
            RuleFor(x => x.DelayAfterDownloadSeconds).InclusiveBetween(0, 3600);

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

        RuleFor(x => x.Seed)
            .NotEmpty()
            .SetValidator(new AppSettings.SeedSection.SeedSectionValidator());

        RuleFor(x => x).Custom((settings, context) =>
        {
            var seedProvider = string.IsNullOrWhiteSpace(settings.Seed.Provider)
                ? settings.Downloader.Provider
                : settings.Seed.Provider;

            if (string.Equals(seedProvider, GamdlSongFileProvider.Name, StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(settings.Downloader.Gamdl.CookiesPath))
            {
                context.AddFailure(
                    "DOWNLOADER__GAMDL__COOKIES_PATH",
                    "DOWNLOADER__GAMDL__COOKIES_PATH is required when seeding with gamdl.");
            }

        });
    }
}