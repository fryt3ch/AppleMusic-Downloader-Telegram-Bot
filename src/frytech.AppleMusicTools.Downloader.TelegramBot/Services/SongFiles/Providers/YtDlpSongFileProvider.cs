using System.Diagnostics;
using System.Globalization;
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

    private const string PythonExecutable = "python3";
    private const string BridgeScriptPath = "/app/ytdlp-bridge.py";
    private const string OutputExtension = ".m4a";
    
    public override string ProviderName => Name;

    private readonly AppSettings _appSettings;
    private readonly ILogger<YtDlpSongFileProvider> _logger;

    public YtDlpSongFileProvider(IOptions<AppSettings> appSettings, AppleMusicCatalog catalog, ILogger<YtDlpSongFileProvider> logger)
        : base(catalog)
    {
        _logger = logger;
        _appSettings = appSettings.Value;
    }
    
    protected override async Task<SongFile> GetSongFileAsync(Song song, CancellationToken cancellationToken)
    {
        var trackStream = await DownloadTrackAsStreamAsync(
            song.Attributes.ArtistName,
            song.Attributes.Name,
            song.Attributes.Duration.TotalSeconds);

        var fileName = SongFileNameBuilder.BuildFileName(song, OutputExtension);
        var songFile = new SongFile(song, fileName, trackStream, ProviderName);

        return songFile;
    }

    private async Task<Stream> DownloadTrackAsStreamAsync(string artist, string title, double durationSeconds)
    {
        var tempDir = Path.GetTempPath();
        var uniqueFileName = $"{Guid.NewGuid()}{OutputExtension}";
        var fullOutputPath = Path.Combine(tempDir, uniqueFileName);

        var cookiesPath = _appSettings.Downloader.YtDlp.CookiesPath ?? string.Empty;

        var startInfo = new ProcessStartInfo
        {
            FileName = PythonExecutable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = tempDir,
        };

        startInfo.ArgumentList.Add(BridgeScriptPath);
        startInfo.ArgumentList.Add(artist);
        startInfo.ArgumentList.Add(title);
        startInfo.ArgumentList.Add(durationSeconds.ToString(CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add(fullOutputPath);
        startInfo.ArgumentList.Add(cookiesPath);

        using (var process = new Process())
        {
            process.StartInfo = startInfo;
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            _logger.LogTrace(output);

            if (process.ExitCode != 0 || !output.Contains("SUCCESS"))
            {
                if (File.Exists(fullOutputPath))
                    File.Delete(fullOutputPath);

                if (output.Contains("NOT_FOUND"))
                    throw new InvalidOperationException($"yt-dlp could not find a matching track for '{artist} - {title}'.");

                throw new InvalidOperationException($"yt-dlp script failed.\nExitCode: {process.ExitCode}\nOutput: {output}\nError: {error}");
            }
        }

        if (!File.Exists(fullOutputPath))
            throw new FileNotFoundException($"yt-dlp script was SUCCESS, but output file was not found at: {fullOutputPath}");

        var fileStream = new FileStream(
            fullOutputPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.None,
            bufferSize: 4096,
            options: FileOptions.DeleteOnClose | FileOptions.Asynchronous
        );

        return fileStream;
    }
}
