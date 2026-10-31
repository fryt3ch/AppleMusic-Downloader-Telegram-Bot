using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Relationships;

/// <summary>
/// An object that represents the album relationship for a Resource object.
/// https://developer.apple.com/documentation/applemusicapi/albumrelationship
/// </summary>
/// <inheritdoc/>
public class AlbumRelationship : Relationship<Album>;