using System.Text.Json.Serialization;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Relationships;

/// <summary>
/// An object that represents the track relationship for a Resource object.
/// https://developer.apple.com/documentation/applemusicapi/trackrelationship
/// </summary>
/// <inheritdoc />
public class TrackRelationship : Relationship
{
    [JsonIgnore]
    public List<Song> SongsData => GetDataOfType<Song>();

    [JsonIgnore]
    public List<MusicVideo> MusicVideoData => GetDataOfType<MusicVideo>();
}