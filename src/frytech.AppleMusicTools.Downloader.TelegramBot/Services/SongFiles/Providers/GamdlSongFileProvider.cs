using frytech.AppleMusic.API.Models.Resources;
using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Catalog;
using Microsoft.Extensions.Options;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.SongFiles.Providers;

[SongFileProvider(Name)]
public sealed class GamdlSongFileProvider : SongFileProviderBase
{
    public const string Name = "gamdl";

    private const string BridgeScriptPath = "/app/gamdl-bridge.py";

    public override string ProviderName => Name;

    private readonly AppSettings _appSettings;
    private readonly PythonProcessRunner _runner;
    private readonly ILogger<GamdlSongFileProvider> _logger;

    public GamdlSongFileProvider(
        IOptions<AppSettings> appSettings,
        AppleMusicCatalog catalog,
        SongFileProviderLimiter limiter,
        PythonProcessRunner runner,
        ILogger<GamdlSongFileProvider> logger)
        : base(catalog, limiter)
    {
        _appSettings = appSettings.Value;
        _runner = runner;
        _logger = logger;
    }

    protected override async Task<SongFile> GetSongFileAsync(Song song, string store, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Downloading song {SongId} '{Artist} - {Title}' via {Provider}", song.Id, song.Attributes.ArtistName, song.Attributes.Name, ProviderName);

        var songUrl = AppleMusicUrl.Create(song.Type, song.Id, string.IsNullOrWhiteSpace(store) ? _appSettings.AppleMusic.DefaultStore : store);
        var trackStream = await DownloadTrackAsStreamAsync(songUrl.AbsoluteUri, cancellationToken);

        var fileName = SongFileNameBuilder.BuildFileName(song);
        var songFile = new SongFile(song, fileName, trackStream, ProviderName);

        return songFile;
    }

    private async Task<Stream> DownloadTrackAsStreamAsync(string songUrl, CancellationToken cancellationToken)
    {
        var tempDir = Path.GetTempPath();
        var uniqueFileName = $"{Guid.NewGuid()}.m4a";
        var fullOutputPath = Path.Combine(tempDir, uniqueFileName);

        var result = await _runner.RunAsync(
            BridgeScriptPath,
            [songUrl, fullOutputPath, _appSettings.Downloader.Gamdl.CookiesPath ?? string.Empty],
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(result.StandardError))
            _logger.LogWarning("gamdl stderr for {SongUrl}: {Error}", songUrl, result.StandardError);

        if (result.ExitCode != 0 || !result.StandardOutput.Contains("SUCCESS") || !File.Exists(fullOutputPath))
        {
            if (File.Exists(fullOutputPath))
                File.Delete(fullOutputPath);

            throw new InvalidOperationException($"gamdl bridge failed.\nExitCode: {result.ExitCode}\nOutput: {result.StandardOutput}\nError: {result.StandardError}");
        }

        _logger.LogDebug("gamdl bridge produced {OutputPath}", fullOutputPath);

        return new FileStream(
            fullOutputPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.None,
            bufferSize: 4096,
            options: FileOptions.DeleteOnClose | FileOptions.Asynchronous);
    }
}
