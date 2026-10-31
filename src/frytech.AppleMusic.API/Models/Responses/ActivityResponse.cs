using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Responses;

/// <summary>
/// The response to an activity request.
/// https://developer.apple.com/documentation/applemusicapi/activityresponse
/// </summary>
/// <inheritdoc />
public class ActivityResponse : DataResponseRoot<Activity>;