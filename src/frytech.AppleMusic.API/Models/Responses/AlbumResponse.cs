using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Responses;

/// <summary>
/// The response to an album request.
/// https://developer.apple.com/documentation/applemusicapi/albumresponse
/// </summary>
/// <inheritdoc />
public class AlbumResponse : DataResponseRoot<Album>;