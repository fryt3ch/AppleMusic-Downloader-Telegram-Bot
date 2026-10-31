using frytech.AppleMusic.API.Models.Core;

namespace frytech.AppleMusic.API.Models.Results;

public class SearchResultGroup : DataResponseRoot<Resource>
{
    public string GroupId { get; set; }
    
    public string Name { get; set; }
}