using frytech.AppleMusic.API.Models.Attributes;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusic.API.Models.Relationships;

namespace frytech.AppleMusic.API.Models.Resources;

/// <summary>
/// A Resource object that represents an Apple curator.
/// https://developer.apple.com/documentation/applemusicapi/applecurator
/// </summary>
/// <inheritdoc />
public class AppleCurator : Resource, IHasAttributes<AppleCuratorAttributes>, IHasRelationships<AppleCuratorRelationships>
{
    public override ResourceType Type => ResourceType.AppleCurators;
    
    public AppleCuratorAttributes Attributes { get; set; }
    
    public AppleCuratorRelationships Relationships { get; set; }
}