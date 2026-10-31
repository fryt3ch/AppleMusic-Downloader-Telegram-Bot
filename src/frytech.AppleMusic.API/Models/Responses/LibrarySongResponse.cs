using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Responses;

/// <summary>
/// The response to a library song request.
/// https://developer.apple.com/documentation/applemusicapi/librarysongresponse
/// </summary>
/// <inheritdoc />
public class LibrarySongResponse : DataResponseRoot<LibrarySong>;