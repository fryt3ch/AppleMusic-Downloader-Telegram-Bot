using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Responses;

/// <summary>
/// The response to a library artist request.
/// https://developer.apple.com/documentation/applemusicapi/libraryartistresponse
/// </summary>
/// <inheritdoc />
public class LibraryArtistResponse : DataResponseRoot<LibraryArtist>;