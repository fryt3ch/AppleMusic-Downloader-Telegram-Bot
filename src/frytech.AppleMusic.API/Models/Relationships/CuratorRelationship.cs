using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Relationships;

/// <summary>
/// An object that represents the curator relationship for a Resource object.
/// https://developer.apple.com/documentation/applemusicapi/curatorrelationship
/// </summary>
public class CuratorRelationship : Relationship<Curator>;