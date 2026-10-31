using frytech.AppleMusic.API.Models.Attributes;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusic.API.Models.Relationships;

namespace frytech.AppleMusic.API.Models.Resources;

/// <summary>
/// A Resource object that represents an album.
/// https://developer.apple.com/documentation/applemusicapi/album
/// </summary>
/// <inheritdoc />
public class Album : Resource, IHasAttributes<AlbumAttributes>, IHasRelationships<AlbumRelationships>
{
    public override ResourceType Type => ResourceType.Albums;
    
    public AlbumAttributes Attributes { get; set; }
    
    public AlbumRelationships Relationships { get; set; }
}