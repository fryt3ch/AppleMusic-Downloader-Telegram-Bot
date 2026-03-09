using System.Text.RegularExpressions;
using frytech.AppleMusic.API.Extensions;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services;

public class UpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _client;
    private readonly MusicService _musicService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UpdateHandler> _logger;
    private readonly SearchResultElementInlineQueryResultProvider _inlineQueryResultProvider;
    private readonly AppSettings _appSettings;

    private static Dictionary<long, CancellationTokenSource> PendingDownloadsUsers = [];

    public UpdateHandler(ITelegramBotClient client, MusicService musicService, IServiceProvider serviceProvider, ILogger<UpdateHandler> logger,
        SearchResultElementInlineQueryResultProvider inlineQueryResultProvider, IOptions<AppSettings> appSettings)
    {
        _client = client;
        _musicService = musicService;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _inlineQueryResultProvider = inlineQueryResultProvider;
        _appSettings = appSettings.Value;
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        var store = _appSettings.AppleMusic.DefaultStore;
        
        if (update.Type == UpdateType.InlineQuery)
        {
            if (update.InlineQuery is null)
                return;
            
            var offset = ParseQueryOffset(update.InlineQuery.Offset);
            var nextOffset = 0;
            var limit = 10;

            var query = update.InlineQuery.Query.Trim();

            if (query.Length == 0)
                return;

            var queryType = QueryType.SearchAll;

            if (TryProceedQuery(query, out var keyword, out var payload))
            {
                queryType = keyword switch
                {
                    "all" => QueryType.SearchSongs,
                    "song" => QueryType.SearchSongs,
                    "album" => QueryType.SearchAlbums,
                    "artist" => QueryType.SearchAuthors,
                    "playlist" => QueryType.SearchPlaylists,
                    
                    "album-songs" => QueryType.ListAlbumSongs,
                    "playlist-songs" => QueryType.ListPlaylistSongs,
                    "artist-songs" => QueryType.ListAuthorSongs,
                    "artist-albums" => QueryType.ListAuthorAlbums,
                    "artist-playlists" => QueryType.ListAuthorPlaylists,
                    
                    _ => queryType
                };
            }
            else
            {
                payload = query;
            }

            if (payload.Length == 0)
                return;

            if (queryType is QueryType.ListAlbumSongs or QueryType.ListPlaylistSongs or QueryType.ListAuthorSongs or QueryType.ListAuthorAlbums or QueryType.ListAuthorPlaylists)
            {
                IEnumerable<Resource> searchResult = queryType switch
                {
                    QueryType.ListAlbumSongs => await _musicService.GetAlbumTracksAsync(offset, limit, payload, store),
                    QueryType.ListPlaylistSongs => await _musicService.GetPlaylistTracksAsync(offset, limit, payload, store),
                    QueryType.ListAuthorSongs => await _musicService.GetAuthorTracksAsync(offset, limit, payload, store),
                    QueryType.ListAuthorAlbums => await _musicService.GetAuthorAlbumsAsync(offset, limit, payload, store),
                    QueryType.ListAuthorPlaylists => await _musicService.GetAuthorPlaylistsAsync(offset, limit, payload, store),
                    
                    _ => throw new InvalidOperationException(),
                };

                var searchResultList = searchResult.ToList();

                if (searchResultList.Count >= limit)
                    nextOffset = offset + searchResultList.Count;
                await _client.AnswerInlineQuery(
                    update.InlineQuery.Id,
                    searchResultList.Select(song => _inlineQueryResultProvider.Provide(song)),
                    nextOffset: MapQueryOffsetToString(nextOffset),
                    cancellationToken: cancellationToken);
            }
            else if (queryType is QueryType.SearchAll or QueryType.SearchAuthors or QueryType.SearchSongs or QueryType.SearchAlbums or QueryType.SearchPlaylists)
            {
                var searchMode = queryType switch
                {
                    QueryType.SearchAll => SearchMode.All,
                    QueryType.SearchSongs => SearchMode.Songs,
                    QueryType.SearchAlbums => SearchMode.Albums,
                    QueryType.SearchAuthors => SearchMode.Authors,
                    QueryType.SearchPlaylists => SearchMode.Playlists,
            
                    _ => throw new InvalidOperationException(),
                };
                
                var searchResult = await _musicService.SearchAsync(payload, offset, limit, searchMode, store);
                var searchResultList = searchResult.ToArray();

                if (searchResultList.Length >= limit)
                    nextOffset = offset + searchResultList.Length;
                
                await _client.AnswerInlineQuery(
                    update.InlineQuery.Id,
                    searchResultList.Select(x => _inlineQueryResultProvider.Provide(x)),
                    nextOffset: MapQueryOffsetToString(nextOffset),
                    cancellationToken: cancellationToken);
            }
        }
        else if (update.Type == UpdateType.CallbackQuery)
        {
            var data = update.CallbackQuery?.Data;
            var message = update.CallbackQuery?.Message;
            
            if (data is not null && message is not null)
            {
                var replyParameters = new ReplyParameters() { AllowSendingWithoutReply = true, MessageId = message.MessageId, };
                
                if (data.StartsWith("ar_"))
                {
                    var artistId = data.Replace("ar_", string.Empty);

                    await ShowArtist(artistId, store, message.Chat, contextMessage: message, contextUserMessage: null, replyParameters: null, cancellationToken);
                }
                else if (data.StartsWith("al_"))
                {
                    var albumId = data.Replace("al_", string.Empty);

                    await ShowAlbum(albumId, store, message.Chat, contextMessage: message, contextUserMessage: null, replyParameters: null, cancellationToken);
                }
                else if (data.StartsWith("dl_"))
                {
                    ICollection<string> songsIds;
                    
                    if (data.StartsWith("dl_s_"))
                    {
                        var songId = data.Replace("dl_s_", string.Empty);

                        songsIds = [songId];
                    }
                    else if (data.StartsWith("dl_al_"))
                    {
                        var albumId = data.Replace("dl_al_", string.Empty);
                        
                        songsIds =  _musicService.GetAlbumTracksAsync(offset: 0, limit: 250, albumId, store).Result.Where(x => x.Type is ResourceType.Songs).Select(x => x.Id).ToArray();
                    }
                    else if (data.StartsWith("dl_pl_"))
                    {
                        var playlistId = data.Replace("dl_pl_", string.Empty);
                        
                        songsIds =  _musicService.GetPlaylistTracksAsync(offset: 0, limit: 250, playlistId, store).Result.Where(x => x.Type is ResourceType.Songs).Select(x => x.Id).ToArray();
                    }
                    else
                    {
                        throw new InvalidOperationException("Unknown DL type.");
                    }
                    
                    await SendSongs(message, replyParameters, songsIds, store, limit: true, cancellationToken);
                }
            }
        }
        else
        {
            if (update.Message?.Entities?.Where(x => x.Type == MessageEntityType.BotCommand).Any() is true)
            {
                if (update.Message?.EntityValues?.Where(x => x == "/start").Any() is true)
                {
                    var replyMarkup = new InlineKeyboardMarkup()
                        .AddButton(new InlineKeyboardButton("Search") { SwitchInlineQueryCurrentChat = $" " })
                        .AddButton(new InlineKeyboardButton("Song") { SwitchInlineQueryCurrentChat = $"song: " })
                        .AddButton(new InlineKeyboardButton("Artist") { SwitchInlineQueryCurrentChat = $"artist: " })
                        .AddButton(new InlineKeyboardButton("Album") { SwitchInlineQueryCurrentChat = $"album: " })
                        .AddButton(new InlineKeyboardButton("Playlist") { SwitchInlineQueryCurrentChat = $"playlist: " });
                    
                    await _client.SendMessage(
                        update.Message.Chat.Id,
                        text: "Press the desired button below and append some text to make a search query.\n\nExample (All): @AppleTools_bot The Beatles\nExample (Song): @AppleTools_bot song: Moonlight Shadow\n\nIf you press 'Search', you'll get everything - songs, artists, albums, playlists.",
                        replyMarkup: replyMarkup,
                        cancellationToken: cancellationToken);
                }
                
                return;
            }
            
            if (update.Message?.Type == MessageType.Text)
            {
                var message = update.Message;
                var chat = message.Chat;
                
                var replyParameters = new ReplyParameters() { AllowSendingWithoutReply = true, MessageId = message.MessageId, };

                if (!Uri.TryCreate(message.Text, UriKind.Absolute, out var targetUrl))
                {
                    await _client.SendMessage(
                        chat.Id,
                        "Wrong URL format!",
                        replyParameters: replyParameters,
                        cancellationToken: cancellationToken);
                    
                    return;
                }

                if (!_musicService.TryGetMusicElementInfo(targetUrl, out _, out var resourceType, out var musicElementId))
                {
                    await _client.SendMessage(
                        chat.Id,
                        "Wrong URL format! Expected something like: https://music.apple.com/us/song/...",
                        replyParameters: replyParameters,
                        cancellationToken: cancellationToken);
                    
                    return;
                }
                
                switch (resourceType)
                {
                    case ResourceType.Songs:
                        await SendSongs(message, replyParameters, [musicElementId], store, limit: false, cancellationToken);
                        break;
                    
                    case ResourceType.Albums:
                        await ShowAlbum(musicElementId, store, chat, contextMessage: null, contextUserMessage: message, replyParameters, cancellationToken);
                        break;
                    
                    case ResourceType.Artists:
                        await ShowArtist(musicElementId, store, chat, contextMessage: null, contextUserMessage: message, replyParameters, cancellationToken);
                        break;
                    
                    case ResourceType.Playlists:
                        await ShowPlaylist(musicElementId, store, chat, contextMessage: null, contextUserMessage: message, replyParameters, cancellationToken);
                        break;
                    
                    default:
                        throw new InvalidOperationException("Unknown music element type.");
                }
            }
        }
    }

    private static bool TryProceedQuery(string query, out string keyword, out string payload)
    {
        keyword = null!;
        payload = null!;
        
        var regex = new Regex(@"^(.+):\s*(.*)$");

        query = query.Trim();

        var match = regex.Match(query);

        if (match.Success && match.Groups.Count >= 3)
        {
            keyword = match.Groups[1].Value;
            payload = match.Groups[2].Value;

            return true;
        }

        return false;
    }

    private static int ParseQueryOffset(string? offsetStr)
    {
        if (int.TryParse(offsetStr, out var offset))
        {
            return offset;
        }

        return 0;
    }
    
    private static string? MapQueryOffsetToString(int offset)
    {
        return offset > 0 ? offset.ToString() : null;
    }

    private async Task ShowArtist(string artistId, string store, Chat chat, Message? contextMessage, Message? contextUserMessage, ReplyParameters? replyParameters, CancellationToken cancellationToken)
    {
        var artist = await _musicService.GetArtistAsync(artistId, store);
                    
        var caption = $"{artist.Attributes.Name}";
        var replyMarkup = new InlineKeyboardMarkup()
            .AddButton(new InlineKeyboardButton("Songs") { SwitchInlineQueryCurrentChat = $"artist-songs: {artist.Id}" })
            .AddButton(new InlineKeyboardButton("Albums") { SwitchInlineQueryCurrentChat = $"artist-albums: {artist.Id}" })
            .AddButton(new InlineKeyboardButton("Playlists") { SwitchInlineQueryCurrentChat = $"artist-playlists: {artist.Id}" });

        if (contextMessage is not null)
            await _client.DeleteMessage(chat.Id, contextMessage.MessageId, cancellationToken);
                    
        if (artist.Attributes.Artwork is null)
        {
            await _client.SendMessage(
                chat.Id,
                caption,
                replyParameters: replyParameters,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
        }
        else
        {
            await _client.SendPhoto(
                chat.Id,
                InputFile.FromUri(artist.Attributes.Artwork.GetImageUrl(_appSettings.Telegram.ThumbnailSize)),
                caption: caption,
                replyParameters: replyParameters,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
        }
    }

    private async Task ShowSong(string songId, string store, Chat chat, Message? contextMessage, Message? contextUserMessage, ReplyParameters? replyParameters, CancellationToken cancellationToken)
    {
        var song = await _musicService.GetSongAsync(songId, store);
                    
        var caption = $"{song.Attributes.ArtistName} - {song.Attributes.Name}\n\nAlbum: {song.Attributes.AlbumName}\n{song.Attributes.ReleaseDate:yyyy.MM.dd}";
        var replyMarkup = new InlineKeyboardMarkup();

        if (song.Attributes.Duration > TimeSpan.Zero)
            replyMarkup.AddButton(new InlineKeyboardButton("Get") { CallbackData = $"dl_s_{song.Id}" });

        replyMarkup.AddNewRow(
            new InlineKeyboardButton("Album") { CallbackData = $"al_{song.Relationships.Albums.Data!.First().Id}" },
            new InlineKeyboardButton("Artist") { CallbackData = $"ar_{song.Relationships.Artists.Data!.First().Id}" });
        

        if (contextMessage is not null)
            await _client.DeleteMessage(chat.Id, contextMessage.MessageId, cancellationToken);
                    
        if (song.Attributes.Artwork is null)
        {
            await _client.SendMessage(
                chat.Id,
                caption,
                replyParameters: replyParameters,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
        }
        else
        {
            await _client.SendPhoto(
                chat.Id,
                InputFile.FromUri(song.Attributes.Artwork.GetImageUrl(_appSettings.Telegram.ThumbnailSize)),
                caption: caption,
                replyParameters: replyParameters,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
        }
    }

    private async Task ShowAlbum(string albumId, string store, Chat chat, Message? contextMessage, Message? contextUserMessage, ReplyParameters? replyParameters, CancellationToken cancellationToken)
    {
        var album = await _musicService.GetAlbumAsync(albumId, store);

        var caption = $"{album.Attributes.ArtistName} - {album.Attributes.Name}\n\n{album.Attributes.TrackCount} tracks\n{album.Attributes.ReleaseDate:yyyy.MM.dd}";
        var replyMarkup = new InlineKeyboardMarkup()
            .AddButton(new InlineKeyboardButton("Songs") { SwitchInlineQueryCurrentChat = $"album-songs: {album.Id}" });

        if (album.Relationships.Artists.Data?.FirstOrDefault()?.Id is { } artistId)
            replyMarkup.AddButton(new InlineKeyboardButton("Artist") { CallbackData = $"ar_{artistId}" });

        replyMarkup.AddNewRow(new InlineKeyboardButton("Get All Songs") { CallbackData = $"dl_al_{album.Id}" });
        
        if (contextMessage is not null)
            await _client.DeleteMessage(chat.Id, contextMessage.MessageId, cancellationToken);
                    
        if (album.Attributes.Artwork is null)
        {
            await _client.SendMessage(
                chat.Id,
                caption,
                replyParameters: replyParameters,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
        }
        else
        {
            await _client.SendPhoto(
                chat.Id,
                InputFile.FromUri(album.Attributes.Artwork.GetImageUrl(_appSettings.Telegram.ThumbnailSize)),
                caption: caption,
                replyParameters: replyParameters,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
        }
    }

    private async Task ShowPlaylist(string playlistId, string store, Chat chat, Message? contextMessage, Message? contextUserMessage, ReplyParameters? replyParameters, CancellationToken cancellationToken)
    {
        var playlist = await _musicService.GetPlaylistAsync(playlistId, store);
                    
        var caption = $"{playlist.Attributes.Name}\n\nCurator: {playlist.Attributes.CuratorName}";
        var replyMarkup = new InlineKeyboardMarkup()
            .AddButton(new InlineKeyboardButton("Songs") { SwitchInlineQueryCurrentChat = $"playlist-songs: {playlist.Id}" });
        
        replyMarkup.AddNewRow(new InlineKeyboardButton("Get All Songs") { CallbackData = $"dl_pl_{playlist.Id}" });
        
        if (contextMessage is not null)
            await _client.DeleteMessage(chat.Id, contextMessage.MessageId, cancellationToken);

        if (playlist.Attributes.Artwork is null)
        {
            await _client.SendMessage(
                chat.Id,
                caption,
                replyParameters: replyParameters,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
        }
        else
        {
            await _client.SendPhoto(
                chat.Id,
                InputFile.FromUri(playlist.Attributes.Artwork.GetImageUrl(_appSettings.Telegram.ThumbnailSize)),
                caption: caption,
                replyParameters: replyParameters,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
        }
    }

    private async Task SendSongs(Message message, ReplyParameters replyParameters, IEnumerable<string> songs, string store, bool limit, CancellationToken cancellationToken)
    {
        var user = message.From;
        
        if (user is null)
            throw new InvalidOperationException("Anonymous downloads are not allowed.");
        
        var scope = _serviceProvider.CreateScope();

        if (!limit)
        {
            var songsGatherer = scope.ServiceProvider.GetRequiredService<SongSender>();
                            
            await songsGatherer.SendSongs(message.Chat, songs, store, replyParameters, cancellationToken);

            return;
        }

        if (PendingDownloadsUsers.ContainsKey(user.Id))
            await _client.SendMessage(message.Chat.Id, "You already have some pending requests!", replyParameters: replyParameters, cancellationToken: cancellationToken);
        
        var downloadCts = new CancellationTokenSource();
        PendingDownloadsUsers.Add(user.Id, downloadCts);
                    
        Task.Run(async () =>
        {
            try
            {
                var songsGatherer = scope.ServiceProvider.GetRequiredService<SongSender>();
                            
                await songsGatherer.SendSongs(message.Chat, songs, store, replyParameters, downloadCts.Token);
            }
            catch (Exception e)
            {
                await _client.SendMessage(message.Chat.Id, "Something went wrong with your latest songs request!");

                _logger.LogError(e, message: null);
            }
            finally
            {
                scope.Dispose();
                            
                if (PendingDownloadsUsers.Remove(user.Id, out var cts))
                    await cts.CancelAsync();
            }
        });
    }
}