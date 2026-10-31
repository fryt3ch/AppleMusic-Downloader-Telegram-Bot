using frytech.AppleMusic.API.Models.Core;

namespace frytech.AppleMusic.API.Models.Attributes;

/// <summary>
/// The attributes for a library playlist creation request object.
/// https://developer.apple.com/documentation/applemusicapi/libraryplaylistcreationrequest/attributes
/// </summary>
public class LibraryPlaylistCreationRequestAttributes : IAttributes, IHasNameAttribute
{
    /// <summary>
    /// The description of the playlist.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// (Required) The name of the playlist.
    /// </summary>
    public string Name { get; set; }
}