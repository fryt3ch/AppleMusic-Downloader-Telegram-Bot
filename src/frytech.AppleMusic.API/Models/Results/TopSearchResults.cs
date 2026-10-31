using frytech.AppleMusic.API.Models.Core;

namespace frytech.AppleMusic.API.Models.Results;

public class TopSearchResults : IResults
{
    public SearchResultGroup? Top { get; set; }
}