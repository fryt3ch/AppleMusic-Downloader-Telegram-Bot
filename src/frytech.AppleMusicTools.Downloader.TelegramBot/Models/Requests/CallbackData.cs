namespace frytech.AppleMusicTools.Downloader.TelegramBot.Models.Requests;

public abstract record CallbackData
{
    private const string ArtistPrefix = "ar_";
    private const string AlbumPrefix = "al_";
    private const string DownloadSongPrefix = "dl_s_";
    private const string DownloadAlbumPrefix = "dl_al_";
    private const string DownloadPlaylistPrefix = "dl_pl_";
    private const string CancelDownloadPrefix = "cancel_dl_";
    private const char OffsetSeparator = ':';

    public static string ForArtist(string id) => $"{ArtistPrefix}{id}";

    public static string ForAlbum(string id) => $"{AlbumPrefix}{id}";

    public static string ForDownloadSong(string id) => $"{DownloadSongPrefix}{id}";

    public static string ForDownloadAlbum(string id, int offset = 0) => CreatePaged(DownloadAlbumPrefix, id, offset);

    public static string ForDownloadPlaylist(string id, int offset = 0) => CreatePaged(DownloadPlaylistPrefix, id, offset);

    public static string ForCancelDownload(string downloadId) => $"{CancelDownloadPrefix}{downloadId}";

    private static string CreatePaged(string prefix, string id, int offset)
    {
        return offset > 0 ? $"{prefix}{id}{OffsetSeparator}{offset}" : $"{prefix}{id}";
    }

    public static bool TryParse(string data, out CallbackData callbackData)
    {
        if (TryExtract(data, ArtistPrefix, id => new ShowArtist(id), out callbackData))
            return true;

        if (TryExtract(data, AlbumPrefix, id => new ShowAlbum(id), out callbackData))
            return true;

        if (TryExtract(data, DownloadSongPrefix, id => new DownloadSong(id), out callbackData))
            return true;

        if (TryExtractPaged(data, DownloadAlbumPrefix, (id, offset) => new DownloadAlbum(id, offset), out callbackData))
            return true;

        if (TryExtractPaged(data, DownloadPlaylistPrefix, (id, offset) => new DownloadPlaylist(id, offset), out callbackData))
            return true;

        if (TryExtract(data, CancelDownloadPrefix, id => new CancelDownload(id), out callbackData))
            return true;

        return false;
    }

    private static bool TryExtract(string data, string prefix, Func<string, CallbackData> factory, out CallbackData callbackData)
    {
        callbackData = null!;

        if (!data.StartsWith(prefix, StringComparison.Ordinal))
            return false;

        var id = data[prefix.Length..];

        if (id.Length == 0)
            return false;

        callbackData = factory(id);

        return true;
    }

    private static bool TryExtractPaged(string data, string prefix, Func<string, int, CallbackData> factory, out CallbackData callbackData)
    {
        callbackData = null!;

        if (!data.StartsWith(prefix, StringComparison.Ordinal))
            return false;

        var payload = data[prefix.Length..];
        var separatorIndex = payload.LastIndexOf(OffsetSeparator);
        var id = separatorIndex >= 0 ? payload[..separatorIndex] : payload;
        var offset = 0;

        if (separatorIndex >= 0 && !int.TryParse(payload[(separatorIndex + 1)..], out offset))
            return false;

        if (id.Length == 0 || offset < 0)
            return false;

        callbackData = factory(id, offset);

        return true;
    }

    public sealed record ShowArtist(string Id) : CallbackData;

    public sealed record ShowAlbum(string Id) : CallbackData;

    public sealed record DownloadSong(string Id) : CallbackData;

    public sealed record DownloadAlbum(string Id, int Offset) : CallbackData;

    public sealed record DownloadPlaylist(string Id, int Offset) : CallbackData;

    public sealed record CancelDownload(string DownloadId) : CallbackData;
}
