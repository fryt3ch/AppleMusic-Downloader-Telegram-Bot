using frytech.AppleMusic.API.Models.Core;

namespace frytech.AppleMusic.API.Models.Responses;

/// <summary>
/// The response to a recently added request.
/// https://developer.apple.com/documentation/applemusicapi/recentlyaddedresponse
/// </summary>
public class RecentlyAddedResponse : DataResponseRoot<Resource>;