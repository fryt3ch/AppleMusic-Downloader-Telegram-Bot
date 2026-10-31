using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Responses;

/// <summary>
/// The response to a rating request.
/// https://developer.apple.com/documentation/applemusicapi/ratingresponse
/// </summary>
/// <inheritdoc />
public class RatingResponse : DataResponseRoot<Rating>;