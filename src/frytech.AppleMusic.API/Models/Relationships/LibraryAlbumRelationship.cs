using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Relationships;

/// <summary>
/// The relationships for a library artist object.
/// https://developer.apple.com/documentation/applemusicapi/libraryartist/relationships
/// </summary>
public class LibraryAlbumRelationship : Relationship<LibraryAlbum>;