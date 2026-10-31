using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Responses;

/// <summary>
/// The response to a library album request.
/// https://developer.apple.com/documentation/applemusicapi/libraryalbumresponse
/// </summary>
/// <inheritdoc />
public class LibraryAlbumResponse : DataResponseRoot<LibraryAlbum>;