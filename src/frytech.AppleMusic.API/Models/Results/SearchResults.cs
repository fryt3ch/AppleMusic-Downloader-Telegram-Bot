using System.Text.Json.Serialization;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Responses;

namespace frytech.AppleMusic.API.Models.Results;

/// <summary>
/// An object that represents the results of a catalog search query.
/// https://developer.apple.com/documentation/applemusicapi/searchresults
/// </summary>
public class SearchResults : IResults
{
    /// <summary>
    /// The activities returned for the search query.
    /// </summary>
    public ActivityResponse Activities { get; set; }

    /// <summary>
    /// The albums returned for the search query.
    /// </summary>
    public AlbumResponse Albums { get; set; }

    /// <summary>
    /// The Apple curators returned for the search query.
    /// </summary>
    [JsonPropertyName("apple-curators")]
    public AppleCuratorResponse AppleCurators { get; set; }

    /// <summary>
    /// The artists returned for the search query.
    /// </summary>
    public ArtistResponse Artists { get; set; }

    /// <summary>
    /// The curators returned for the search query.
    /// </summary>
    public CuratorResponse Curators { get; set; }

    /// <summary>
    /// The music videos returned for the search query.
    /// </summary>
    [JsonPropertyName("music-videos")]
    public MusicVideoResponse MusicVideos { get; set; }

    /// <summary>
    /// The playlists returned for the search query.
    /// </summary>
    public PlaylistResponse Playlists { get; set; }

    /// <summary>
    /// The songs returned for the search query.
    /// </summary>
    public SongResponse Songs { get; set; }

    /// <summary>
    /// The stations returned for the search query.
    /// </summary>
    public StationResponse Stations { get; set; }
}