namespace frytech.AppleMusicTools.Downloader.TelegramBot.Models.Requests;

public enum QueryType
{
    SearchAll = 0,
    SearchSongs,
    SearchAlbums,
    SearchAuthors,
    SearchPlaylists,
    ListAlbumSongs,
    ListPlaylistSongs,
    ListAuthorSongs,
    ListAuthorAlbums,
    ListAuthorPlaylists,
}