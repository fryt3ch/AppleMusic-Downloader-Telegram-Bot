using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Responses;

/// <summary>
/// The response to a genre request.
/// https://developer.apple.com/documentation/applemusicapi/genreresponse
/// </summary>
/// <inheritdoc />
public class GenreResponse : DataResponseRoot<Genre>;