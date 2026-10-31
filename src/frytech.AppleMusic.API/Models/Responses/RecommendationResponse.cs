using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Responses;

/// <summary>
/// The response to a recommendation request.
/// https://developer.apple.com/documentation/applemusicapi/recommendationresponse
/// </summary>
/// <inheritdoc />
public class RecommendationResponse : DataResponseRoot<Recommendation>;