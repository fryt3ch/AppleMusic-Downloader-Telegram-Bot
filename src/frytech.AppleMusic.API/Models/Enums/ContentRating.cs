using System.Text.Json.Serialization;

namespace frytech.AppleMusic.API.Models.Enums;

public enum ContentRating
{
    None,
    [JsonStringEnumMemberName("clean")]
    Clean,
    [JsonStringEnumMemberName("explicit")]
    Explicit
}