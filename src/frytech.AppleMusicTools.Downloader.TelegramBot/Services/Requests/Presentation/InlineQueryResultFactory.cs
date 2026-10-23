using frytech.AppleMusic.API.Extensions;
using frytech.AppleMusic.API.Models.Attributes;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusic.API.Models.Resources;
using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Resources;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Catalog;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types.InlineQueryResults;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Requests.Presentation;

public class InlineQueryResultFactory
{
    private readonly AppSettings _appSettings;

    public InlineQueryResultFactory(IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value;
    }
    
    public InlineQueryResult Provide(Resource resource)
    {
        if (resource is not IHasAttributes<IAttributes> hasAttributesResource)
            throw new InvalidOperationException("Resource is not an IHasAttributes!");

        var attributes = hasAttributesResource.Attributes;

        var url = AppleMusicUrl.Create(resource.ResourceType, resource.Id, _appSettings.AppleMusic.DefaultStore);
        var urlStr = url.ToString();
        
        var inputMessageContent = new InputTextMessageContent($"{urlStr}");
        var article = new InlineQueryResultArticle(Guid.NewGuid().ToString(), (attributes as IHasNameAttribute)?.Name ?? string.Empty, inputMessageContent);

        if (attributes is IHasArtworkAttribute { Artwork: not null } hasArtworkAttribute)
            article.ThumbnailUrl = hasArtworkAttribute.Artwork.GetImageUrl(_appSettings.Telegram.ThumbnailPreviewSize).ToString();

        if (attributes is ITrackAttributes trackAttributes)
        {
            var duration = trackAttributes.Duration == TimeSpan.Zero
                ? TelegramMessages.InlineNotReleased
                : string.Format(
                    TelegramMessages.InlineDuration,
                    trackAttributes.Duration.Minutes,
                    trackAttributes.Duration.Seconds);

            article.Description = resource.ResourceType is ResourceType.MusicVideos
                ? string.Format(TelegramMessages.InlineMusicVideoDescription, trackAttributes.ArtistName)
                : string.Format(TelegramMessages.InlineTrackDescription, trackAttributes.ArtistName, duration);
        }
        else
            article.Description = resource switch
            {
                Album album => string.Format(
                    TelegramMessages.InlineAlbumDescription,
                    album.Attributes.ArtistName,
                    album.Attributes.ReleaseDate,
                    album.Attributes.TrackCount),
                Artist artist => string.Join(", ", artist.Attributes.GenreNames ?? []),
                Playlist playlist => string.Format(
                    TelegramMessages.InlinePlaylistDescription,
                    playlist.Attributes.CuratorName,
                    playlist.Attributes.LastModifiedDate.ToString("yyyy.MM.dd")),

                _ => article.Description
            };

        return article;
    }
}