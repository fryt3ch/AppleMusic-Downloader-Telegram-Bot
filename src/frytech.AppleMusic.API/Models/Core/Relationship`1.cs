
namespace frytech.AppleMusic.API.Models.Core;

/// <summary>
/// A to-one or to-many relationship from one resource object to others.
/// https://developer.apple.com/documentation/applemusicapi/relationship
/// </summary>
public class Relationship<TResource>
    where TResource : Resource
{
    /// <summary>
    /// One or more destination objects.
    /// </summary>
    public List<TResource>? Data { get; set; }

    /// <summary>
    /// A URL subpath that fetches the resource as the primary object. This member is only present in responses.
    /// </summary>
    public string? Href { get; set; }

    /// <summary>
    /// Link to the next page of resources in the relationship. Contains the offset query parameter that specifies the next page. See Fetch Resources by Page.
    /// </summary>
    public string? Next { get; set; }
    
    // TODO - MJP - figure out meta schema
    /// <summary>
    /// Information about the request or response. The members may be any of the endpoint parameters.
    /// </summary>
    public object? Meta { get; set; }

    /// <summary>
    /// Get Resources of a specific Type from the Data collection.
    /// Only required when the Data collections may contain multiple Types.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    protected List<T>? GetDataOfType<T>()
    {
        return Data?.OfType<T>().ToList();
    }
}