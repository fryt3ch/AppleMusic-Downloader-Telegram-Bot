using frytech.AppleMusic.API.Models.Core;

namespace frytech.AppleMusic.API.Models.Results;

/// <summary>
/// An object that represents the autocompletion options for the hint.
/// https://developer.apple.com/documentation/applemusicapi/searchhints
/// </summary>
public class SearchHints : IResults
{
    /// <summary>
    /// (Required) The autocomplete options derived from the search hint.
    /// </summary>
    public List<string> Terms { get; set; }
}