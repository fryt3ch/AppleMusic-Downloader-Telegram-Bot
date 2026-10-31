using frytech.AppleMusic.API.Models.Attributes;

namespace frytech.AppleMusic.API.Models.Requests;

public class RatingRequest
{
    public RatingAttributes Attributes { get; set; }

    public string Type => "rating";
}