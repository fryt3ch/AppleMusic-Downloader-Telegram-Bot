
using frytech.AppleMusic.API.Extensions;
using frytech.AppleMusic.API.Models.Attributes;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusic.API.Models.Resources;
using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types.InlineQueryResults;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services;

public class SearchResultElementInlineQueryResultProvider
{
    private readonly MusicService _musicService;
    private readonly AppSettings _appSettings;

    public SearchResultElementInlineQueryResultProvider(MusicService musicService, IOptions<AppSettings> appSettings)
    {
        _musicService = musicService;
        _appSettings = appSettings.Value;
    }
    
    public InlineQueryResult Provide(Resource resource)
    {
        if (resource is not IHasAttributes<IAttributes> hasAttributesResource)
            throw new InvalidOperationException("Resource is not an IHasAttributes!");

        var attributes = hasAttributesResource.Attributes;

        var url = _musicService.CreateMusicElementUrl(resource.Type, resource.Id, _appSettings.AppleMusic.DefaultStore);
        var urlStr = url.ToString();
        
        var inputMessageContent = new InputTextMessageContent($"{urlStr}");
        var article = new InlineQueryResultArticle(Guid.NewGuid().ToString(), (attributes as IHasNameAttribute)?.Name ?? string.Empty, inputMessageContent);

        if (attributes is IHasArtworkAttribute { Artwork: not null } hasArtworkAttribute)
            article.ThumbnailUrl = hasArtworkAttribute.Artwork.GetImageUrl(_appSettings.Telegram.ThumbnailPreviewSize).ToString();

        if (attributes is ITrackAttributes trackAttributes)
        {
            article.Description = resource.Type is ResourceType.MusicVideos
                ? $"{trackAttributes.ArtistName}\nMusic Videos Are Not Available!"
                : $"{trackAttributes.ArtistName}\n{(trackAttributes.Duration == TimeSpan.Zero
                    ? "Not Released"
                    : $"{trackAttributes.Duration.Minutes} min. {trackAttributes.Duration.Seconds} sec.")}";
        }
        else
            article.Description = resource switch
            {
                Album album => $"{album.Attributes.ArtistName}\n{album.Attributes.ReleaseDate} | {album.Attributes.TrackCount} tracks",
                Artist artist => $"{string.Join(", ", artist.Attributes.GenreNames ?? [])}",
                Playlist playlist => $"{playlist.Attributes.CuratorName}\n{playlist.Attributes.LastModifiedDate:yyyy.MM.dd}",
                
                _ => article.Description
            };

        return article;
    }
}