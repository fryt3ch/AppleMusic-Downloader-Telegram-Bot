using System.Text.Json.Serialization;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;
using frytech.AppleMusic.API.Models.Responses;

namespace frytech.AppleMusic.API.Models.Results;

public class LibrarySearchResults : IResults
{
    [JsonPropertyName("library-albums")]
    public List<LibraryAlbum> LibraryAlbums { get; set; }

    [JsonPropertyName("library-artists")]
    public List<LibraryArtist> LibraryArtists { get; set; }

    [JsonPropertyName("library-music-videos")]
    public List<LibraryMusicVideo> LibraryMusicVideos { get; set; }

    [JsonPropertyName("library-playlists")]
    public List<LibraryPlaylistResponse> LibraryPlaylists { get; set; }

    [JsonPropertyName("library-songs")]
    public List<LibrarySong> LibrarySongs { get; set; }
}