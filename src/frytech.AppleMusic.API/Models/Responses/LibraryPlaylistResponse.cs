using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Responses;

/// <summary>
/// The response to a library playlist request.
/// https://developer.apple.com/documentation/applemusicapi/libraryplaylistresponse
/// </summary>
/// <inheritdoc />
public class LibraryPlaylistResponse : DataResponseRoot<LibraryPlaylist>;