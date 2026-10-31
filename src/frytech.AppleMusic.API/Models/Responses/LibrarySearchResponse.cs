using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Results;

namespace frytech.AppleMusic.API.Models.Responses;

/// <summary>
/// The response to a library search request.
/// https://developer.apple.com/documentation/applemusicapi/librarysearchresponse
/// </summary>
/// <inheritdoc />
public class LibrarySearchResponse : SearchResponseRoot<LibrarySearchResults>;