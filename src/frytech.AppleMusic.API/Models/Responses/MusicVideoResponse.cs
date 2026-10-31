using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Responses;

/// <summary>
/// The response to a music video request.
/// https://developer.apple.com/documentation/applemusicapi/musicvideoresponse
/// </summary>
/// <inheritdoc />
public class MusicVideoResponse : DataResponseRoot<MusicVideo>;