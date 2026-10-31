using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Responses;

/// <summary>
/// The response to a library music video request.
/// https://developer.apple.com/documentation/applemusicapi/librarymusicvideoresponse
/// </summary>
/// <inheritdoc />
public class LibraryMusicVideoResponse : DataResponseRoot<LibraryMusicVideo>;