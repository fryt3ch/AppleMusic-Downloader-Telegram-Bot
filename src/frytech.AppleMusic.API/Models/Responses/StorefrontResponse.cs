using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Responses;

/// <summary>
/// The response to a storefront request.
/// https://developer.apple.com/documentation/applemusicapi/storefrontresponse
/// </summary>
/// <inheritdoc />
public class StorefrontResponse : DataResponseRoot<Storefront>;