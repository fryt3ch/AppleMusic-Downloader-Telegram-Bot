using System.Text.Json.Serialization;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Relationships;

/// <summary>
/// An object that represents the library track relationship for a Resource object.
/// https://developer.apple.com/documentation/applemusicapi/librarytrackrelationship
/// </summary>
/// <inheritdoc />
public class LibraryTrackRelationship : Relationship
{
    [JsonIgnore]
    public List<LibrarySong> LibrarySongsData => GetDataOfType<LibrarySong>();

    [JsonIgnore]
    public List<LibraryMusicVideo> LibraryMusicVideosData => GetDataOfType<LibraryMusicVideo>();
}