using frytech.AppleMusic.API.Models.Attributes;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusic.API.Models.Relationships;

namespace frytech.AppleMusic.API.Models.Resources;

/// <summary>
/// A Resource object that represents recommended resources for a user calculated using their selected preferences.
/// https://developer.apple.com/documentation/applemusicapi/recommendation
/// </summary>
/// <inheritdoc />
public class Recommendation : Resource, IHasAttributes<RecommendationAttributes>, IHasRelationships<RecommendationRelationships>
{
    public override ResourceType Type => ResourceType.Recommendation;
    
    public RecommendationAttributes Attributes { get; set; }
    
    public RecommendationRelationships Relationships { get; set; }
}