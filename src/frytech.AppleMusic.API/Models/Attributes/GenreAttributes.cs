using frytech.AppleMusic.API.Models.Core;

namespace frytech.AppleMusic.API.Models.Attributes;

/// <summary>
/// The attributes for a genre object.
/// https://developer.apple.com/documentation/applemusicapi/genre/attributes
/// </summary>
public class GenreAttributes : IAttributes, IHasNameAttribute
{
    /// <summary>
    /// (Required) The localized name of the genre.
    /// </summary>
    public string Name { get; set; }
}