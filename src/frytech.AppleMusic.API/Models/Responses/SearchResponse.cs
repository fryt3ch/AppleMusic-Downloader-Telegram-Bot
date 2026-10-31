using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Results;

namespace frytech.AppleMusic.API.Models.Responses;

/// <summary>
/// The response to a search request.
/// https://developer.apple.com/documentation/applemusicapi/searchresponse
/// </summary>
/// <inheritdoc />
public class SearchResponse : SearchResponseRoot<SearchResults>;