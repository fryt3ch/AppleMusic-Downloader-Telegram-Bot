using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Relationships;

/// <summary>
/// An object that represents the music video relationship for a Resource object.
/// https://developer.apple.com/documentation/applemusicapi/musicvideorelationship
/// </summary>
public class MusicVideoRelationship : Relationship<MusicVideo>;