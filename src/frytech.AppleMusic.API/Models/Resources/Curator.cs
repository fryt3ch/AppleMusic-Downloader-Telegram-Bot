using frytech.AppleMusic.API.Models.Attributes;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusic.API.Models.Relationships;

namespace frytech.AppleMusic.API.Models.Resources;

/// <summary>
/// A Resource object that represents a curator of resources.
/// https://developer.apple.com/documentation/applemusicapi/curator
/// </summary>
/// <inheritdoc />
public class Curator : Resource, IHasAttributes<CuratorAttributes>, IHasRelationships<CuratorRelationships>
{
    public override ResourceType Type => ResourceType.Curators;
    
    public CuratorAttributes Attributes { get; set; }
    
    public CuratorRelationships Relationships { get; set; }
}