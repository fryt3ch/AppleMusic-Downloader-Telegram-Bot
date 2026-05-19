using System.Diagnostics;
using frytech.AppleMusic.API.Models.Resources;
using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models;
using Microsoft.Extensions.Options;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services;

public sealed class GamdlSongFileProvider : ISongFileProvider
{
    private const string PythonExecutable = "python3";
    private const string BridgeScriptPath = "gamdl-bridge.py";
    
    private readonly AppSettings _appSettings;
    private readonly MusicService _musicService;
    private readonly ILogger<GamdlSongFileProvider> _logger;

    public GamdlSongFileProvider(IOptions<AppSettings> appSettings, MusicService musicService, ILogger<GamdlSongFileProvider> logger)
    {
        _musicService = musicService;
        _logger = logger;
        _appSettings = appSettings.Value;
    }
    
    public async Task<SongFile> GetSongFileAsync(Song song)
    {
        var songUrl = _musicService.CreateMusicElementUrl(song.ResourceType, song.Id);
        var trackStream = await DownloadTrackAsStreamAsync(songUrl.AbsoluteUri);

        var fileName = SongFileNameBuilder.BuildFileName(song);
        var songFile = new SongFile(fileName, trackStream);

        return songFile;
    }
    
    private async Task<Stream> DownloadTrackAsStreamAsync(string songUrl)
    {
        var tempDir = Path.GetTempPath();
        var uniqueFileName = $"{Guid.NewGuid()}.m4a";
        var fullOutputPath = Path.Combine(tempDir, uniqueFileName);
        
        var arguments = $"\"{BridgeScriptPath}\" \"{songUrl}\" \"{fullOutputPath}\" \"{_appSettings.AppleMusicDownloader.CookiesPath}\"";

        var startInfo = new ProcessStartInfo
        {
            FileName = PythonExecutable,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = tempDir,
        };

        using (var process = new Process())
        {
            process.StartInfo = startInfo;
            process.Start();
            
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            
            _logger.LogTrace(output);
            
            if (process.ExitCode != 0 || !string.IsNullOrEmpty(error))
            {
                if (File.Exists(fullOutputPath))
                    File.Delete(fullOutputPath);
                
                throw new InvalidOperationException($"Python script failed.\nExitCode: {process.ExitCode}\nError: {error}");
            }
        }
        
        if (!File.Exists(fullOutputPath))
            throw new FileNotFoundException($"Gamdl script was SUCCESS, but output file was not found at: {fullOutputPath}");
        
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