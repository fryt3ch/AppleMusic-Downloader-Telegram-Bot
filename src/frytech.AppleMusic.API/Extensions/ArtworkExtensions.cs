using frytech.AppleMusic.API.Models.Attributes;

namespace frytech.AppleMusic.API.Extensions;

public static class ArtworkExtensions
{
    public static Uri GetImageUrl(this Artwork artwork, int width, int height)
    {
        return GetImageUrlWithSize(artwork.Url, width, height);
    }
    
    public static Uri GetImageUrl(this Artwork artwork, int size)
    {
        return GetImageUrlWithSize(artwork.Url, size, size);
    }
    
    public static Uri GetImageUrl(this Artwork artwork)
    {
        return GetImageUrlWithSize(artwork.Url, artwork.Width, artwork.Height);
    }

    private static Uri GetImageUrlWithSize(Uri thumbnailTemplateUrl, int width, int height)
    {
        return new Uri(thumbnailTemplateUrl.ToString().Replace("{w}", width.ToString()).Replace("{h}", height.ToString()));
    }
}