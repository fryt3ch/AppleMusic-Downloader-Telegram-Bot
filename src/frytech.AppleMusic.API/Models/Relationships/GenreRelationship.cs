using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Relationships;

/// <summary>
/// An object that represents the genre relationship for a Resource object.
/// https://developer.apple.com/documentation/applemusicapi/genrerelationship
/// </summary>
/// <inheritdoc />
public class GenreRelationship : Relationship<Genre>;