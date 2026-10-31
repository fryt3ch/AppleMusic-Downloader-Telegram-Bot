using System.Text.Json;
using System.Text.Json.Serialization;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Seeding;

public sealed class SeedState
{
    public int Version { get; set; } = 1;

    public string ArtistsFile { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Dictionary<string, SeedArtistState> Artists { get; set; } = new(StringComparer.Ordinal);

    public long TotalProcessedSongs { get; set; }

    public long TotalFailedSongs { get; set; }
}

public sealed class SeedArtistState
{
    public string Name { get; set; } = string.Empty;

    public string? AppleMusicId { get; set; }

    public string Status { get; set; } = SeedArtistStatus.Pending;

    public int Attempts { get; set; }

    public string? Error { get; set; }

    public int AlbumsProcessed { get; set; }

    public int SongsQueued { get; set; }

    public int SongsProcessed { get; set; }

    public int SongsFailed { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public static class SeedArtistStatus
{
    public const string Pending = "pending";
    public const string Done = "done";
    public const string Partial = "partial";
    public const string Skipped = "skipped";
    public const string Failed = "failed";
}

public sealed class SeedStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly SemaphoreSlim _saveGate = new(1, 1);
    private readonly ILogger<SeedStateStore> _logger;

    public SeedStateStore(ILogger<SeedStateStore> logger)
    {
        _logger = logger;
    }

    public async Task<SeedState> LoadAsync(string path, string artistsFile, CancellationToken cancellationToken)
    {
        var resolvedPath = SeedPathResolver.ResolveWritePath(path);

        if (!File.Exists(resolvedPath))
            return new SeedState { ArtistsFile = artistsFile };

        try
        {
            await using var stream = File.OpenRead(resolvedPath);
            var state = await JsonSerializer.DeserializeAsync<SeedState>(stream, JsonOptions, cancellationToken);

            if (state is null)
                return new SeedState { ArtistsFile = artistsFile };

            state.Artists ??= new Dictionary<string, SeedArtistState>(StringComparer.Ordinal);
            state.ArtistsFile = artistsFile;

            _logger.LogInformation(
                "Loaded seed state from {Path}: {ArtistCount} artists, {ProcessedSongs} processed songs, {FailedSongs} failed songs",
                resolvedPath,
                state.Artists.Count,
                state.TotalProcessedSongs,
                state.TotalFailedSongs);

            return state;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read seed state from {Path}; starting with an empty state", resolvedPath);

            return new SeedState { ArtistsFile = artistsFile };
        }
    }

    public async Task SaveAsync(string path, SeedState state, CancellationToken cancellationToken)
    {
        await _saveGate.WaitAsync(cancellationToken);

        try
        {
            var resolvedPath = SeedPathResolver.ResolveWritePath(path);
            SeedPathResolver.EnsureParentDirectory(resolvedPath);

            state.UpdatedAt = DateTimeOffset.UtcNow;

            var tempPath = $"{resolvedPath}.{Environment.ProcessId}.tmp";

            await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, state, JsonOptions, cancellationToken);
            }

            File.Move(tempPath, resolvedPath, overwrite: true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save seed state to {Path}", path);
        }
        finally
        {
            _saveGate.Release();
        }
    }
}
