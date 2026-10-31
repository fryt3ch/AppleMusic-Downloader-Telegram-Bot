using System.Text.Json.Serialization;

namespace frytech.AppleMusic.API.Models.Enums.Relationships;

public enum LibraryArtistRelationshipType
{
    [JsonStringEnumMemberName("albums")]
    Albums
}