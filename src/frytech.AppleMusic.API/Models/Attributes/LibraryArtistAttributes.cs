using frytech.AppleMusic.API.Models.Core;

namespace frytech.AppleMusic.API.Models.Attributes;

/// <summary>
/// The attributes for a library artist object.
/// https://developer.apple.com/documentation/applemusicapi/libraryartist/attributes
/// </summary>
public class LibraryArtistAttributes : IAttributes, IHasNameAttribute
{
    /// <summary>
    /// (Required) The artist’s name.
    /// </summary>
    public string Name { get; set; }
}