using frytech.AppleMusic.API.Models.Attributes;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Enums;

namespace frytech.AppleMusic.API.Models.Resources;

/// <summary>
/// A Resource object that represents a station.
/// https://developer.apple.com/documentation/applemusicapi/station
/// </summary>
/// <inheritdoc />
public class Station : Resource, IHasAttributes<StationAttributes>
{
    public override ResourceType Type => ResourceType.Stations;
    
    public StationAttributes Attributes { get; set; }
}