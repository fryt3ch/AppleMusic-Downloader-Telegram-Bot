using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Responses;

/// <summary>
/// The response to a song request.
/// https://developer.apple.com/documentation/applemusicapi/songresponse
/// </summary>
/// <inheritdoc />
public class SongResponse : DataResponseRoot<Song>;