using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Enums;

namespace frytech.AppleMusic.API.Models.Attributes;

/// <summary>
/// The attributes for a library album object.
/// https://developer.apple.com/documentation/applemusicapi/libraryalbum/attributes
/// </summary>
public class LibraryAlbumAttributes : IAttributes, IHasNameAttribute, IHasArtworkAttribute
{
    /// <summary>
    /// (Required) The artist’s name.
    /// </summary>
    public string ArtistName { get; set; }

    /// <summary>
    /// (Required) The album artwork.
    /// </summary>
    public Artwork Artwork { get; set; }

    /// <summary>
    /// The Recording Industry Association of America (RIAA) rating of the content. The possible values for this rating are clean and explicit. No value means no rating.
    /// </summary>
    public ContentRating ContentRating { get; set; }

    /// <summary>
    /// (Required) The localized name of the album.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The parameters to use to playback the tracks of the album.
    /// </summary>
    public PlayParameters PlayParams { get; set; }

    /// <summary>
    /// (Required) The number of tracks.
    /// </summary>
    public int TrackCount { get; set; }
}