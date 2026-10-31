using frytech.AppleMusic.API.Models.Attributes;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusic.API.Models.Relationships;

namespace frytech.AppleMusic.API.Models.Resources;

/// <summary>
/// A Resource object that represents an artist of an album where an artist can be one or more persons.
/// https://developer.apple.com/documentation/applemusicapi/artist
/// </summary>
/// <inheritdoc />
public class Artist : Resource, IHasAttributes<ArtistAttributes>, IHasRelationships<ArtistRelationships>
{
    public override ResourceType Type => ResourceType.Artists;
    
    public ArtistAttributes Attributes { get; set; }
    
    public ArtistRelationships Relationships { get; set; }
}