using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Responses;

/// <summary>
/// The response to an Apple curator request.
/// https://developer.apple.com/documentation/applemusicapi/applecuratorresponse
/// </summary>
/// <inheritdoc />
public class AppleCuratorResponse : DataResponseRoot<AppleCurator>;