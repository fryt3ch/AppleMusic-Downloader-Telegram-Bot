using frytech.AppleMusic.API.Models.Attributes;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Enums;

namespace frytech.AppleMusic.API.Models.Resources;

/// <summary>
/// A Resource object that represents a storefront, an iTunes Store territory that the content is available in.
/// https://developer.apple.com/documentation/applemusicapi/storefront
/// </summary>
/// <inheritdoc />
public class Storefront : Resource, IHasAttributes<StorefrontAttributes>
{
    public override ResourceType Type => ResourceType.Storefronts;
    
    public StorefrontAttributes Attributes { get; set; }
}