using frytech.AppleMusic.API.Models.Attributes;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Enums;

namespace frytech.AppleMusic.API.Models.Resources;

/// <summary>
/// An object that represents a rating for a Resource.
/// https://developer.apple.com/documentation/applemusicapi/rating
/// </summary>
public class Rating : Resource, IHasAttributes<RatingAttributes>
{
    public override ResourceType Type => ResourceType.Ratings;
    
    public RatingAttributes Attributes { get; set; }
}