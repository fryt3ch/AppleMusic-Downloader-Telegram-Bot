using System.Text.Json.Serialization;
using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Core;

/// <summary>
/// A resource—such as an album, song, or playlist—in the Apple Music catalog or iCloud Music Library.
/// https://developer.apple.com/documentation/applemusicapi/resource
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Activity), "activities")]
[JsonDerivedType(typeof(Album), "albums")]
[JsonDerivedType(typeof(AppleCurator), "apple-curators")]
[JsonDerivedType(typeof(Artist), "artists")]
[JsonDerivedType(typeof(Curator), "curators")]
[JsonDerivedType(typeof(Genre), "genres")]
[JsonDerivedType(typeof(MusicVideo), "music-videos")]
[JsonDerivedType(typeof(Playlist), "playlists")]
[JsonDerivedType(typeof(Rating), "ratings")]
[JsonDerivedType(typeof(Recommendation), "personal-recommendation")]
[JsonDerivedType(typeof(Song), "songs")]
[JsonDerivedType(typeof(Station), "stations")]
[JsonDerivedType(typeof(Storefront), "storefronts")]
[JsonDerivedType(typeof(LibraryAlbum), "library-albums")]
[JsonDerivedType(typeof(LibraryArtist), "library-artists")]
[JsonDerivedType(typeof(LibraryMusicVideo), "library-music-videos")]
[JsonDerivedType(typeof(LibraryPlaylist), "library-playlists")]
[JsonDerivedType(typeof(LibrarySong), "library-songs")]
public abstract class Resource
{
    /// <summary>
    /// (Required) The type of resource.
    /// </summary>
    [JsonIgnore]
    public abstract ResourceType Type { get; }
    
    /// <summary>
    /// (Required) Persistent identifier of the resource.
    /// </summary>
    public string Id { get; set; }
    
    /// <summary>
    /// A URL subpath that fetches the resource as the primary object. This member is only present in responses.
    /// </summary>
    public string? Href { get; set; }
    
    // TODO - MJP - Need a solution for serializing this. Find out content structure.
    /// <summary>
    /// Information about the request or response. The members may be any of the endpoint parameters.
    /// </summary>
    public object? Meta { get; set; }
}