namespace frytech.AppleMusicTools.Downloader.TelegramBot.Models.Requests;

public abstract record CallbackData
{
    private const string ArtistPrefix = "ar_";
    private const string AlbumPrefix = "al_";
    private const string DownloadSongPrefix = "dl_s_";
    private const string DownloadAlbumPrefix = "dl_al_";
    private const string DownloadPlaylistPrefix = "dl_pl_";

    public static string ForArtist(string id) => $"{ArtistPrefix}{id}";

    public static string ForAlbum(string id) => $"{AlbumPrefix}{id}";

    public static string ForDownloadSong(string id) => $"{DownloadSongPrefix}{id}";

    public static string ForDownloadAlbum(string id) => $"{DownloadAlbumPrefix}{id}";

    public static string ForDownloadPlaylist(string id) => $"{DownloadPlaylistPrefix}{id}";

    public static bool TryParse(string data, out CallbackData callbackData)
    {
        if (TryExtract(data, ArtistPrefix, id => new ShowArtist(id), out callbackData))
            return true;

        if (TryExtract(data, AlbumPrefix, id => new ShowAlbum(id), out callbackData))
            return true;

        if (TryExtract(data, DownloadSongPrefix, id => new DownloadSong(id), out callbackData))
            return true;

        if (TryExtract(data, DownloadAlbumPrefix, id => new DownloadAlbum(id), out callbackData))
            return true;

        if (TryExtract(data, DownloadPlaylistPrefix, id => new DownloadPlaylist(id), out callbackData))
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

    public sealed record ShowArtist(string Id) : CallbackData;

    public sealed record ShowAlbum(string Id) : CallbackData;

    public sealed record DownloadSong(string Id) : CallbackData;

    public sealed record DownloadAlbum(string Id) : CallbackData;

    public sealed record DownloadPlaylist(string Id) : CallbackData;
}
