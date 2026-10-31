using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Responses;

/// <summary>
/// The response to a playlist request.
/// https://developer.apple.com/documentation/applemusicapi/playlistresponse
/// </summary>
/// <inheritdoc />
public class PlaylistResponse : DataResponseRoot<Playlist>;