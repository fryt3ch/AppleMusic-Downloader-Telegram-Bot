using frytech.AppleMusic.API.Models.Resources;
using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Catalog;
using Microsoft.Extensions.Options;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.SongFiles.Providers;

[SongFileProvider(Name)]
public sealed class YtDlpSongFileProvider : SongFileProviderBase
{
    public const string Name = "ytdlp";

    private const string BridgeScriptPath = "/app/ytdlp-bridge.py";
    private const string OutputExtension = ".m4a";

    public override string ProviderName => Name;

    private readonly AppSettings _appSettings;
    private readonly PythonProcessRunner _runner;
    private readonly ILogger<YtDlpSongFileProvider> _logger;

    public YtDlpSongFileProvider(
        IOptions<AppSettings> appSettings,
        AppleMusicCatalog catalog,
        SongFileProviderLimiter limiter,
        PythonProcessRunner runner,
        ILogger<YtDlpSongFileProvider> logger)
        : base(catalog, limiter)
    {
        _appSettings = appSettings.Value;
        _runner = runner;
        _logger = logger;
    }

    protected override async Task<SongFile> GetSongFileAsync(Song song, string store, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Downloading song {SongId} '{Artist} - {Title}' via {Provider}", song.Id, song.Attributes.ArtistName, song.Attributes.Name, ProviderName);

        var trackStream = await DownloadTrackAsStreamAsync(song.Id, store, cancellationToken);

        var fileName = SongFileNameBuilder.BuildFileName(song, OutputExtension);
        var songFile = new SongFile(song, fileName, trackStream, ProviderName);

        return songFile;
    }

    private async Task<Stream> DownloadTrackAsStreamAsync(string songId, string store, CancellationToken cancellationToken)
    {
        var tempDir = Path.GetTempPath();
        var uniqueFileName = $"{Guid.NewGuid()}{OutputExtension}";
        var fullOutputPath = Path.Combine(tempDir, uniqueFileName);

        var cookiesPath = _appSettings.Downloader.YtDlp.CookiesPath ?? string.Empty;

        var result = await _runner.RunAsync(
            BridgeScriptPath,
            [songId, store, fullOutputPath, cookiesPath],
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(result.StandardError))
            _logger.LogWarning("yt-dlp stderr for song {SongId}: {Error}", songId, result.StandardError);

        if (result.ExitCode != 0 || !result.StandardOutput.Contains("SUCCESS"))
        {
            if (File.Exists(fullOutputPath))
                File.Delete(fullOutputPath);

            if (result.StandardOutput.Contains("NOT_FOUND"))
                throw new InvalidOperationException($"yt-dlp could not find a matching track for Apple Music song '{songId}'.");

            throw new InvalidOperationException($"yt-dlp bridge failed.\nExitCode: {result.ExitCode}\nOutput: {result.StandardOutput}\nError: {result.StandardError}");
        }

        if (!File.Exists(fullOutputPath))
            throw new FileNotFoundException($"yt-dlp script was SUCCESS, but output file was not found at: {fullOutputPath}");

        _logger.LogDebug("yt-dlp bridge produced {OutputPath}", fullOutputPath);

        return new FileStream(
            fullOutputPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.None,
            bufferSize: 4096,
            options: FileOptions.DeleteOnClose | FileOptions.Asynchronous);
    }
}
