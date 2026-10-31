using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Responses;

/// <summary>
/// The response to a curator request.
/// https://developer.apple.com/documentation/applemusicapi/curatorresponse
/// </summary>
/// <inheritdoc />
public class CuratorResponse : DataResponseRoot<Curator>;