using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Results;

namespace frytech.AppleMusic.API.Models.Responses;

/// <summary>
/// The response to a search hints request.
/// https://developer.apple.com/documentation/applemusicapi/searchhintsresponse
/// </summary>
/// <inheritdoc />
public class SearchHintsResponse : SearchResponseRoot<SearchHints>;