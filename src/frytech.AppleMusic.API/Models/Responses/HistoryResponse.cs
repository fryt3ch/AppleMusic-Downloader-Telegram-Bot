using frytech.AppleMusic.API.Models.Core;

namespace frytech.AppleMusic.API.Models.Responses;

/// <summary>
/// The response to a history request.
/// https://developer.apple.com/documentation/applemusicapi/historyresponse
/// </summary>
/// <inheritdoc />
public class HistoryResponse : DataResponseRoot<Resource>;