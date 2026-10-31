using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Enums;

namespace frytech.AppleMusic.API.Models.Attributes;

public interface ITrackAttributes : IAttributes, IHasNameAttribute, IHasArtworkAttribute
{
    /// <summary>
    /// (Required) The name of the album the song appears on.
    /// </summary>
    public string AlbumName { get; }

    /// <summary>
    /// (Required) The artist’s name.
    /// </summary>
    public string ArtistName { get; }
    
    /// <summary>
    /// The duration of the song in milliseconds.
    /// </summary>
    public TimeSpan Duration { get; set; }
    
    /// <summary>
    /// The Recording Industry Association of America (RIAA) rating of the content. The possible values for this rating are clean and explicit. No value means no rating.
    /// </summary>
    public ContentRating ContentRating { get; set; }
    
    /// <summary>
    /// The notes about the song that appear in the iTunes Store.
    /// </summary>
    public EditorialNotes EditorialNotes { get; set; }
    
    /// <summary>
    /// (Required) The music video’s associated genres.
    /// </summary>
    public List<string> GenreNames { get; set; }
    
    /// <summary>
    /// (Required) The International Standard Recording Code (ISRC) for the song.
    /// </summary>
    public string Isrc { get; set; }
    
    /// <summary>
    /// The parameters to use to play back the music video.
    /// </summary>
    public PlayParameters PlayParams { get; set; }

    /// <summary>
    /// (Required) The preview assets for the music video.
    /// </summary>
    public List<Preview> Previews { get; set; }
    
    /// <summary>
    /// (Required) The release date of the song in YYYY-MM-DD format.
    /// </summary>
    public DateTime ReleaseDate { get; set; }

    /// <summary>
    /// (Required) The number of the song in the album’s track list.
    /// </summary>
    public int TrackNumber { get; set; }
    
    /// <summary>
    /// (Required) The URL for sharing a song in the iTunes Store.
    /// </summary>
    public Uri Url { get; set; }
}