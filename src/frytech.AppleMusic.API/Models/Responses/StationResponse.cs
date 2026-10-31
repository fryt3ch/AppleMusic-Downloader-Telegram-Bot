using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Responses;

/// <summary>
/// The response to a station request.
/// https://developer.apple.com/documentation/applemusicapi/stationresponse
/// </summary>
/// <inheritdoc />
public class StationResponse : DataResponseRoot<Station>;