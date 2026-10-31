using frytech.AppleMusic.API.Models.Attributes;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusic.API.Models.Relationships;

namespace frytech.AppleMusic.API.Models.Resources;

/// <summary>
/// A Resource object that represents a music video.
/// https://developer.apple.com/documentation/applemusicapi/musicvideo
/// </summary>
/// <inheritdoc />
public class MusicVideo : Resource, IHasAttributes<MusicVideoAttributes>, IHasRelationships<MusicVideoRelationships>
{
    public override ResourceType Type => ResourceType.MusicVideos;
    
    public MusicVideoAttributes Attributes { get; set; }
    
    public MusicVideoRelationships Relationships { get; set; }
}