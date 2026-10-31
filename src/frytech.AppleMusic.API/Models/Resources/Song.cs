using frytech.AppleMusic.API.Models.Attributes;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusic.API.Models.Relationships;

namespace frytech.AppleMusic.API.Models.Resources;

/// <summary>
/// A Resource object that represents a song.
/// https://developer.apple.com/documentation/applemusicapi/song
/// </summary>
/// <inheritdoc />
public class Song : Resource, IHasAttributes<SongAttributes>, IHasRelationships<SongRelationships>
{
    public override ResourceType Type => ResourceType.Songs;
    
    public SongAttributes Attributes { get; set; }
    
    public SongRelationships Relationships { get; set; }
}