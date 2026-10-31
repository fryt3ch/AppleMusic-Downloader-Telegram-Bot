using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Relationships;

/// <summary>
/// An object that represents the station relationship for a Resource object.
/// https://developer.apple.com/documentation/applemusicapi/stationrelationship
/// </summary>
public class StationRelationship : Relationship<Station>;