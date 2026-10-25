using System.Collections.Concurrent;
using System.Diagnostics;
using frytech.AppleMusic.API.Models.Resources;
using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Exceptions;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Caching;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.SongFiles;
using Microsoft.Extensions.Options;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Seeding;

public sealed class SeedRunner
{
    private readonly IOptions<AppSettings> _appSettings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SeedArtistListLoader _artistListLoader;
    private readonly SeedStateStore _stateStore;
    private readonly SeedReportWriter _reportWriter;
    private readonly ILogger<SeedRunner> _logger;

    public SeedRunner(
        IOptions<AppSettings> appSettings,
        IServiceScopeFactory scopeFactory,
        SeedArtistListLoader artistListLoader,
        SeedStateStore stateStore,
        SeedReportWriter reportWriter,
        ILogger<SeedRunner> logger)
    {
        _appSettings = appSettings;
        _scopeFactory = scopeFactory;
        _artistListLoader = artistListLoader;
        _stateStore = stateStore;
        _reportWriter = reportWriter;
        _logger = logger;
    }

    public async Task<bool> RunAsync(CancellationToken cancellationToken)
    {
        var settings = _appSettings.Value.Seed;
        var providerName = string.IsNullOrWhiteSpace(settings.Provider)
            ? _appSettings.Value.Downloader.Provider
            : settings.Provider;

        providerName = providerName.ToLowerInvariant();

        var store = _appSettings.Value.AppleMusic.DefaultStore;

        using var seedLock = SeedLock.TryAcquire("data/seed.lock", _logger);

        if (seedLock is null)
            return false;

        var allArtists = await _artistListLoader.LoadAsync(cancellationToken);
        var takeCount = settings.MaxArtists > 0 ? settings.MaxArtists : int.MaxValue;
        var selectedArtists = allArtists.Skip(settings.StartIndex).Take(takeCount).ToArray();

        if (selectedArtists.Length == 0)
        {
            _logger.LogInformation("Seed artist selection is empty. StartIndex={StartIndex}, MaxArtists={MaxArtists}", settings.StartIndex, settings.MaxArtists);

            return true;
        }

        var state = settings.DryRun
            ? new SeedState { ArtistsFile = settings.ArtistsFile }
            : await _stateStore.LoadAsync(settings.StatePath, settings.ArtistsFile, cancellationToken);

        var filters = new SeedFilters(settings);
        var processedAppleMusicIds = new HashSet<string>(
            state.Artists.Values
                .Where(artistState => artistState.Status == SeedArtistStatus.Done &&
                                      !string.IsNullOrWhiteSpace(artistState.AppleMusicId))
                .Select(artistState => artistState.AppleMusicId!),
            StringComparer.Ordinal);
        var stats = new SeedRunStats();
        var stopwatch = Stopwatch.StartNew();
        var stoppedByCircuitBreaker = false;

        _logger.LogInformation(
            "Seed started: provider={Provider}, store={Store}, selectedArtists={Selected}/{Total}, startIndex={StartIndex}, maxSongs={MaxSongs}, concurrency={Concurrency}, dryRun={DryRun}, state={StatePath}",
            providerName,
            store,
            selectedArtists.Length,
            allArtists.Count,
            settings.StartIndex,
            settings.MaxSongs == 0 ? "unlimited" : settings.MaxSongs.ToString(),
            settings.Concurrency,
            settings.DryRun,
            settings.StatePath);

        try
        {
            for (var position = 0; position < selectedArtists.Length; position++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (settings.MaxSongs > 0 && stats.SongsAttempted >= settings.MaxSongs)
                {
                    _logger.LogInformation("Seed song limit reached ({MaxSongs}). Stopping.", settings.MaxSongs);

                    break;
                }

                var artist = selectedArtists[position];

                if (!settings.DryRun && ShouldSkipArtist(state, artist, settings))
                {
                    stats.ArtistsSkipped++;

                    continue;
                }

                var artistState = GetOrCreateArtistState(state, artist);

                if (!settings.DryRun)
                    artistState.UpdatedAt = DateTimeOffset.UtcNow;

                _logger.LogInformation("[{Position}/{Total}] Artist '{Name}'", position + 1, selectedArtists.Length, artist.Name);

                ResolvedArtist? resolvedArtist;

                try
                {
                    resolvedArtist = await ResolveArtistAsync(artist, settings, store, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Seed cancelled while resolving artist '{Name}'", artist.Name);

                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to resolve artist '{Name}'", artist.Name);

                    artistState.Attempts++;
                    artistState.Status = SeedArtistStatus.Failed;
                    artistState.Error = ex.Message;
                    artistState.UpdatedAt = DateTimeOffset.UtcNow;

                    stats.ArtistsFailed++;

                    await SaveStateAsync(settings, state);

                    continue;
                }

                if (resolvedArtist is null)
                {
                    stats.ArtistsSkipped++;

                    if (!settings.DryRun)
                    {
                        artistState.Status = SeedArtistStatus.Skipped;
                        artistState.Error = "Apple Music artist match was not found or was ambiguous.";
                        artistState.UpdatedAt = DateTimeOffset.UtcNow;
                    }

                    await SaveStateAsync(settings, state);

                    continue;
                }

                artistState.AppleMusicId = resolvedArtist.Id;
                artistState.Name = artist.Name;

                if (processedAppleMusicIds.Contains(resolvedArtist.Id))
                {
                    _logger.LogInformation(
                        "Skipping '{Name}': Apple Music artist {ArtistId} was already processed under another name",
                        artist.Name,
                        resolvedArtist.Id);

                    stats.ArtistsSkipped++;

                    if (!settings.DryRun)
                    {
                        artistState.Status = SeedArtistStatus.Skipped;
                        artistState.Error = "Duplicate Apple Music artist ID (alias).";
                        artistState.UpdatedAt = DateTimeOffset.UtcNow;
                    }

                    await SaveStateAsync(settings, state);

                    continue;
                }

                processedAppleMusicIds.Add(resolvedArtist.Id);

                _logger.LogInformation(
                    "Matched '{Name}' -> '{MatchedName}' ({ArtistId}, score {Score:0.000})",
                    artist.Name,
                    resolvedArtist.MatchedName,
                    resolvedArtist.Id,
                    resolvedArtist.Score);

                try
                {
                    var result = await ProcessArtistAsync(
                        artist,
                        resolvedArtist,
                        filters,
                        settings,
                        providerName,
                        store,
                        artistState,
                        stats,
                        cancellationToken);

                    if (result.Cancelled)
                    {
                        _logger.LogInformation("Seed cancelled while processing artist '{Name}'", artist.Name);

                        if (!settings.DryRun)
                        {
                            artistState.Status = SeedArtistStatus.Partial;
                            artistState.UpdatedAt = DateTimeOffset.UtcNow;
                        }

                        break;
                    }

                    if (result.CircuitBroken)
                    {
                        stoppedByCircuitBreaker = true;

                        if (!settings.DryRun)
                        {
                            artistState.Status = SeedArtistStatus.Failed;
                            artistState.Error = "Stopped after too many consecutive download failures.";
                            artistState.UpdatedAt = DateTimeOffset.UtcNow;
                        }

                        stats.ArtistsFailed++;

                        break;
                    }

                    if (result.DryRun)
                    {
                        stats.ArtistsProcessed++;
                        stats.DryRunArtists++;
                    }
                    else if (artistState.Status == SeedArtistStatus.Skipped)
                    {
                        stats.ArtistsSkipped++;
                    }
                    else
                    {
                        stats.ArtistsProcessed++;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Seed cancelled while processing artist '{Name}'", artist.Name);

                    if (!settings.DryRun)
                    {
                        artistState.Status = SeedArtistStatus.Partial;
                        artistState.UpdatedAt = DateTimeOffset.UtcNow;
                    }

                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process artist '{Name}'", artist.Name);

                    artistState.Attempts++;
                    artistState.Status = SeedArtistStatus.Failed;
                    artistState.Error = ex.Message;
                    artistState.UpdatedAt = DateTimeOffset.UtcNow;

                    stats.ArtistsFailed++;
                }
                finally
                {
                    await SaveStateAsync(settings, state);
                }
            }
        }
        finally
        {
            stopwatch.Stop();

            await SaveStateAsync(settings, state);

            var elapsed = stopwatch.Elapsed;

            _logger.LogInformation(
                "Seed finished: artists processed={ArtistsProcessed}, skipped={ArtistsSkipped}, failed={ArtistsFailed}; songs processed={SongsProcessed}, failed={SongsFailed}, attempted={SongsAttempted}; elapsed={Elapsed}; rate={Rate:0.00} songs/min{Circuit}",
                stats.ArtistsProcessed,
                stats.ArtistsSkipped,
                stats.ArtistsFailed,
                stats.SongsProcessed,
                stats.SongsFailed,
                stats.SongsAttempted,
                elapsed,
                elapsed.TotalMinutes <= 0 ? 0 : stats.SongsProcessed / elapsed.TotalMinutes,
                stoppedByCircuitBreaker ? "; stopped by circuit breaker" : string.Empty);
        }

        return true;
    }

    private async Task<ArtistProcessResult> ProcessArtistAsync(
        SeedArtistRecord artist,
        ResolvedArtist resolvedArtist,
        SeedFilters filters,
        AppSettings.SeedSection settings,
        string providerName,
        string store,
        SeedArtistState artistState,
        SeedRunStats stats,
        CancellationToken cancellationToken)
    {
        if (!settings.DryRun)
        {
            artistState.SongsProcessed = 0;
            artistState.SongsFailed = 0;
        }

        await using var catalogScope = _scopeFactory.CreateAsyncScope();

        var catalog = catalogScope.ServiceProvider.GetRequiredService<SeedCatalogReader>();

        var allAlbums = await catalog.GetArtistAlbumsAsync(resolvedArtist.Id, store, cancellationToken);
        var albums = allAlbums.Where(filters.ShouldIncludeAlbum).ToArray();
        var songs = new List<Song>();
        var seenSongIds = new HashSet<string>(StringComparer.Ordinal);

        _logger.LogInformation(
            "Artist '{Name}': {SelectedAlbums}/{TotalAlbums} albums selected after filtering",
            artist.Name,
            albums.Length,
            allAlbums.Count);

        var albumSongsById = new ConcurrentDictionary<string, IReadOnlyList<Song>>(StringComparer.Ordinal);

        await Parallel.ForEachAsync(
            albums,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = settings.CatalogConcurrency,
                CancellationToken = cancellationToken,
            },
            async (album, token) =>
            {
                var albumSongs = await catalog.GetAlbumSongsAsync(album.Id, store, token);

                albumSongsById[album.Id] = albumSongs;
            });

        foreach (var album in albums)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!albumSongsById.TryGetValue(album.Id, out var albumSongs))
                continue;

            foreach (var song in albumSongs)
            {
                if (filters.ShouldIncludeSong(song) && seenSongIds.Add(song.Id))
                    songs.Add(song);
            }

            if (settings.MaxSongsPerArtist > 0 && songs.Count >= settings.MaxSongsPerArtist)
                break;
        }

        if (songs.Count == 0 && settings.FallbackArtistSongs && settings.FallbackArtistSongsLimit > 0)
        {
            _logger.LogInformation("Artist '{Name}' has no songs after album filtering; using top songs fallback", artist.Name);

            var fallbackSongs = await catalog.GetArtistTopSongsAsync(
                resolvedArtist.Id,
                store,
                settings.FallbackArtistSongsLimit,
                cancellationToken);

            foreach (var song in fallbackSongs)
            {
                if (filters.ShouldIncludeSong(song) && seenSongIds.Add(song.Id))
                    songs.Add(song);
            }
        }

        artistState.AlbumsProcessed = albums.Length;
        artistState.SongsQueued = songs.Count;

        var truncatedByGlobalLimit = false;

        if (settings.MaxSongsPerArtist > 0 && songs.Count > settings.MaxSongsPerArtist)
            songs = songs.Take(settings.MaxSongsPerArtist).ToList();

        if (settings.MaxSongs > 0)
        {
            var remaining = settings.MaxSongs - stats.SongsAttempted;

            if (remaining <= 0)
                return ArtistProcessResult.Empty(settings.DryRun);

            if (songs.Count > remaining)
            {
                songs = songs.Take(remaining).ToList();
                truncatedByGlobalLimit = true;
            }
        }

        if (songs.Count == 0)
        {
            _logger.LogWarning("Artist '{Name}' has no songs to process", artist.Name);

            if (!settings.DryRun)
            {
                artistState.Status = SeedArtistStatus.Skipped;
                artistState.Error = "No songs after filtering.";
                artistState.UpdatedAt = DateTimeOffset.UtcNow;
            }

            return ArtistProcessResult.Empty(settings.DryRun);
        }

        if (settings.DryRun)
        {
            _logger.LogInformation(
                "Dry run: artist '{Name}' would process {Songs} songs from {Albums} albums",
                artist.Name,
                songs.Count,
                albums.Length);

            return ArtistProcessResult.DryRunCompleted(songs.Count);
        }

        _logger.LogInformation("Artist '{Name}': downloading {Songs} songs", artist.Name, songs.Count);

        var downloadResult = await DownloadSongsAsync(
            artist,
            songs,
            providerName,
            store,
            settings,
            artistState,
            stats,
            cancellationToken);

        artistState.SongsProcessed += downloadResult.Processed;
        artistState.SongsFailed += downloadResult.Failed;
        artistState.UpdatedAt = DateTimeOffset.UtcNow;

        if (downloadResult.Cancelled)
        {
            artistState.Status = SeedArtistStatus.Partial;

            return ArtistProcessResult.CancelledResult();
        }

        if (downloadResult.CircuitBroken)
        {
            artistState.Attempts++;
            artistState.Status = SeedArtistStatus.Failed;
            artistState.Error = "Too many consecutive download failures.";

            return ArtistProcessResult.CircuitBreakerResult();
        }

        if (downloadResult.Failed == 0)
        {
            artistState.Status = SeedArtistStatus.Done;
            artistState.Error = null;
        }
        else
        {
            artistState.Attempts++;

            if (artistState.Attempts >= settings.MaxAttempts)
            {
                artistState.Status = SeedArtistStatus.Failed;
                artistState.Error = $"{downloadResult.Failed} songs failed after {artistState.Attempts} attempts.";
            }
            else
            {
                artistState.Status = SeedArtistStatus.Partial;
                artistState.Error = $"{downloadResult.Failed} songs failed; will retry.";
            }
        }

        if (truncatedByGlobalLimit && artistState.Status == SeedArtistStatus.Done)
        {
            artistState.Status = SeedArtistStatus.Partial;
            artistState.Error = "Global song limit reached; remaining artist songs will be processed on the next run.";
        }

        return ArtistProcessResult.Completed();
    }

    private async Task<DownloadResult> DownloadSongsAsync(
        SeedArtistRecord artist,
        IReadOnlyList<Song> songs,
        string providerName,
        string store,
        AppSettings.SeedSection settings,
        SeedArtistState artistState,
        SeedRunStats stats,
        CancellationToken cancellationToken)
    {
        var counters = new DownloadCounters();
        using var downloadCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            await Parallel.ForEachAsync(
                songs,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = settings.Concurrency,
                    CancellationToken = downloadCts.Token,
                },
                async (song, token) =>
                {
                    var attempted = Interlocked.Increment(ref counters.Attempted);

                    var outcome = await DownloadSongAsync(song, artist.Name, providerName, store, settings, token);

                    if (outcome.Success)
                    {
                        Interlocked.Increment(ref counters.Processed);
                        Interlocked.Exchange(ref counters.ConsecutiveFailures, 0);
                    }
                    else
                    {
                        Interlocked.Increment(ref counters.Failed);

                        var consecutiveFailures = Interlocked.Increment(ref counters.ConsecutiveFailures);

                        await _reportWriter.ReportFailedSongAsync(
                            artist.Name,
                            song.Id,
                            song.Attributes?.Name,
                            outcome.Error ?? "Unknown error",
                            CancellationToken.None);

                        if (consecutiveFailures >= settings.MaxConsecutiveFailures)
                        {
                            if (Interlocked.Exchange(ref counters.CircuitBroken, 1) == 0)
                            {
                                _logger.LogError(
                                    "Stopping seed after {Failures} consecutive download failures. Check gamdl cookies, Apple Music subscription, and rate limits.",
                                    consecutiveFailures);

                                downloadCts.Cancel();
                            }
                        }
                    }

                    if (attempted % 10 == 0)
                    {
                        _logger.LogInformation(
                            "Artist '{Artist}': processed {Processed}/{Total}, failed {Failed}",
                            artist.Name,
                            Volatile.Read(ref counters.Attempted),
                            songs.Count,
                            Volatile.Read(ref counters.Failed));
                    }
                });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new DownloadResult(counters.Processed, counters.Failed, Cancelled: true, CircuitBroken: false);
        }
        catch (OperationCanceledException) when (downloadCts.IsCancellationRequested)
        {
            return new DownloadResult(counters.Processed, counters.Failed, Cancelled: false, CircuitBroken: true);
        }
        finally
        {
            stats.SongsAttempted += counters.Attempted;
            stats.SongsProcessed += counters.Processed;
            stats.SongsFailed += counters.Failed;
            stats.TotalProcessedSongs += counters.Processed;
            stats.TotalFailedSongs += counters.Failed;
        }

        return new DownloadResult(counters.Processed, counters.Failed, Cancelled: false, CircuitBroken: Volatile.Read(ref counters.CircuitBroken) != 0);
    }

    private async Task<DownloadOutcome> DownloadSongAsync(
        Song song,
        string artistName,
        string providerName,
        string store,
        AppSettings.SeedSection settings,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= settings.MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                var storage = scope.ServiceProvider.GetRequiredService<ISongStorage>();
                var provider = scope.ServiceProvider.GetRequiredKeyedService<ISongFileProvider>(providerName);

                await storage.GetOrAddSong(
                    song.Id,
                    store,
                    token => provider.GetSongFileAsync(song.Id, store, token),
                    cancellationToken);

                return DownloadOutcome.Successful();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SongDownloadFailedException ex)
            {
                lastException = ex;
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            if (attempt < settings.MaxAttempts)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));

                _logger.LogWarning(
                    "Download failed for '{Artist} - {Song}' (id: {SongId}): {Error}; retrying in {Delay} (attempt {Attempt}/{MaxAttempts})",
                    artistName,
                    song.Attributes?.Name ?? song.Id,
                    song.Id,
                    lastException?.Message ?? "Unknown error",
                    delay,
                    attempt,
                    settings.MaxAttempts);

                await Task.Delay(delay, cancellationToken);
            }
        }

        return DownloadOutcome.Failed(lastException?.Message ?? "Unknown error");
    }

    private async Task<ResolvedArtist?> ResolveArtistAsync(
        SeedArtistRecord artist,
        AppSettings.SeedSection settings,
        string store,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(artist.AppleMusicId))
            return new ResolvedArtist(artist.AppleMusicId, artist.Name, artist.Name, 1.0);

        await using var scope = _scopeFactory.CreateAsyncScope();

        var catalog = scope.ServiceProvider.GetRequiredService<SeedCatalogReader>();
        var candidates = await catalog.SearchArtistsAsync(artist.Name, store, cancellationToken);

        if (candidates.Count == 0)
        {
            _logger.LogWarning("Apple Music artist not found: '{Name}'", artist.Name);

            await _reportWriter.ReportUnmatchedArtistAsync(artist.Name, null, 0, CancellationToken.None);

            return null;
        }

        var best = candidates
            .Select(candidate => new
            {
                Candidate = candidate,
                Name = candidate.Attributes?.Name ?? candidate.Id,
                Score = ArtistNameMatcher.Score(artist.Name, candidate.Attributes?.Name ?? candidate.Id),
            })
            .OrderByDescending(x => x.Score)
            .First();

        if (best.Score < settings.ArtistMatchThreshold)
        {
            _logger.LogWarning(
                "Apple Music artist match is ambiguous for '{Name}': best candidate '{Candidate}' (score {Score:0.000})",
                artist.Name,
                best.Name,
                best.Score);

            await _reportWriter.ReportUnmatchedArtistAsync(artist.Name, best.Name, best.Score, CancellationToken.None);

            return null;
        }

        return new ResolvedArtist(best.Candidate.Id, artist.Name, best.Name, best.Score);
    }

    private static bool ShouldSkipArtist(SeedState state, SeedArtistRecord artist, AppSettings.SeedSection settings)
    {
        if (!state.Artists.TryGetValue(artist.NormalizedName, out var artistState))
            return false;

        if (artistState.Status == SeedArtistStatus.Done)
        {
            var overrideWasAdded = !string.IsNullOrWhiteSpace(artist.AppleMusicId) &&
                                   !string.Equals(artistState.AppleMusicId, artist.AppleMusicId, StringComparison.Ordinal);

            return !overrideWasAdded;
        }

        if (artistState.Status == SeedArtistStatus.Skipped)
        {
            var overrideWasAdded = !string.IsNullOrWhiteSpace(artist.AppleMusicId) &&
                                   !string.Equals(artistState.AppleMusicId, artist.AppleMusicId, StringComparison.Ordinal);

            return !overrideWasAdded;
        }

        if (artistState.Status is SeedArtistStatus.Failed or SeedArtistStatus.Partial &&
            artistState.Attempts >= settings.MaxAttempts)
            return true;

        return false;
    }

    private static SeedArtistState GetOrCreateArtistState(SeedState state, SeedArtistRecord artist)
    {
        if (!state.Artists.TryGetValue(artist.NormalizedName, out var artistState))
        {
            artistState = new SeedArtistState
            {
                Name = artist.Name,
                AppleMusicId = artist.AppleMusicId,
            };

            state.Artists[artist.NormalizedName] = artistState;
        }
        else
        {
            artistState.Name = artist.Name;
        }

        return artistState;
    }

    private Task SaveStateAsync(AppSettings.SeedSection settings, SeedState state)
    {
        if (settings.DryRun)
            return Task.CompletedTask;

        state.TotalProcessedSongs = state.Artists.Values.Sum(x => (long)x.SongsProcessed);
        state.TotalFailedSongs = state.Artists.Values.Sum(x => (long)x.SongsFailed);

        return _stateStore.SaveAsync(settings.StatePath, state, CancellationToken.None);
    }

    private sealed record ResolvedArtist(string Id, string RequestedName, string MatchedName, double Score);

    private sealed record ArtistProcessResult(bool Cancelled, bool CircuitBroken, bool DryRun, int DryRunSongs)
    {
        public static ArtistProcessResult Completed() => new(false, false, false, 0);

        public static ArtistProcessResult Empty(bool dryRun) => new(false, false, dryRun, 0);

        public static ArtistProcessResult DryRunCompleted(int songs) => new(false, false, true, songs);

        public static ArtistProcessResult CancelledResult() => new(true, false, false, 0);

        public static ArtistProcessResult CircuitBreakerResult() => new(false, true, false, 0);
    }

    private sealed record DownloadResult(int Processed, int Failed, bool Cancelled, bool CircuitBroken);

    private sealed record DownloadOutcome(bool Success, string? Error)
    {
        public static DownloadOutcome Successful() => new(true, null);

        public static DownloadOutcome Failed(string error) => new(false, error);
    }

    private sealed class DownloadCounters
    {
        public int Attempted;
        public int Processed;
        public int Failed;
        public int ConsecutiveFailures;
        public int CircuitBroken;
    }

    private sealed class SeedRunStats
    {
        public int ArtistsProcessed;
        public int ArtistsSkipped;
        public int ArtistsFailed;
        public int DryRunArtists;
        public int SongsAttempted;
        public int SongsProcessed;
        public int SongsFailed;
        public long TotalProcessedSongs;
        public long TotalFailedSongs;
    }
}
