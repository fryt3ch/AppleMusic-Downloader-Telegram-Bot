using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Responses;

/// <summary>
/// The response to an artist request.
/// https://developer.apple.com/documentation/applemusicapi/artistresponse
/// </summary>
/// <inheritdoc />
public class ArtistResponse : DataResponseRoot<Artist>;