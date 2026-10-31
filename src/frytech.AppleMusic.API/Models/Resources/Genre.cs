using frytech.AppleMusic.API.Models.Attributes;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Enums;

namespace frytech.AppleMusic.API.Models.Resources;

/// <summary>
/// An object that represents a genre for resources.
/// https://developer.apple.com/documentation/applemusicapi/genre
/// </summary>
/// <inheritdoc />
public class Genre : Resource, IHasAttributes<GenreAttributes>
{
    public override ResourceType Type => ResourceType.Genres;
    
    public GenreAttributes Attributes { get; set; }
}