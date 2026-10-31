using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Relationships;

/// <summary>
/// An object that represents the library artist relationship for a Resource object.
/// https://developer.apple.com/documentation/applemusicapi/libraryartistrelationship
/// </summary>
/// <inheritdoc />
public class LibraryArtistRelationship : Relationship<LibraryArtist>;