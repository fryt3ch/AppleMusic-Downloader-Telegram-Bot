using System.Diagnostics;
using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using Microsoft.Extensions.Options;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.SongFiles;

public sealed record PythonProcessResult(int ExitCode, string StandardOutput, string StandardError);

public sealed class PythonProcessRunner
{
    private const string PythonExecutable = "python3";

    private readonly ILogger<PythonProcessRunner> _logger;
    private readonly TimeSpan _timeout;

    public PythonProcessRunner(IOptions<AppSettings> appSettings, ILogger<PythonProcessRunner> logger)
    {
        _logger = logger;
        _timeout = TimeSpan.FromSeconds(appSettings.Value.Downloader.ProcessTimeoutSeconds);
    }

    public async Task<PythonProcessResult> RunAsync(
        string scriptPath,
        IReadOnlyCollection<string> arguments,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var startInfo = new ProcessStartInfo
        {
            FileName = PythonExecutable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetTempPath(),
        };

        startInfo.ArgumentList.Add(scriptPath);

        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = startInfo };
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_timeout);

        _logger.LogDebug("Starting Python bridge {Script} with {ArgumentCount} arguments", scriptPath, arguments.Count);

        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            process.Start();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to start Python bridge '{scriptPath}'.", exception);
        }

        using var registration = timeoutCts.Token.Register(() =>
        {
            try
            {
                if (process.HasExited)
                    return;

                _logger.LogWarning("Killing Python bridge {Script} because it was cancelled or timed out", scriptPath);
                process.Kill(entireProcessTree: true);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to kill Python bridge {Script}", scriptPath);
            }
        });

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            try
            {
                await Task.WhenAll(standardOutputTask, standardErrorTask);
            }
            catch
            {
                // The process was killed; output is best-effort.
            }

            if (cancellationToken.IsCancellationRequested)
                throw;

            throw new TimeoutException($"Python bridge '{Path.GetFileName(scriptPath)}' timed out after {_timeout.TotalSeconds:0} seconds.");
        }

        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;

        _logger.LogDebug(
            "Python bridge {Script} exited with code {ExitCode} in {Elapsed}",
            scriptPath,
            process.ExitCode,
            Stopwatch.GetElapsedTime(startedAt));

        return new PythonProcessResult(process.ExitCode, standardOutput, standardError);
    }
}
