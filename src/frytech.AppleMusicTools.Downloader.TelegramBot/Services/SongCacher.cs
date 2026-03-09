using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models.Database;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services;

public class SongCacher : ISongCacher
{
    private readonly ITelegramBotClient _client;
    private readonly AppDbContext _appDbContext;
    private readonly MusicService _musicService;
    private readonly ISongFileProvider _songFileProvider;
    private readonly ChatId _storageChatId;

    public SongCacher(ITelegramBotClient client, IOptions<AppSettings> appSettings, AppDbContext appDbContext, MusicService musicService, ISongFileProvider songFileProvider)
    {
        _client = client;
        _appDbContext = appDbContext;
        _musicService = musicService;
        _songFileProvider = songFileProvider;

        _storageChatId = new ChatId(appSettings.Value.Telegram.StorageChatId);
    }
    
    public async Task<CachedSong> GetCachedSong(string songId, string store, CancellationToken cancellationToken)
    {
        var cachedSong = await _appDbContext.CachedSongs.FindAsync([songId, store], cancellationToken);

        if (cachedSong is null)
        {
            var song = await _musicService.GetSongAsync(songId, store);
            var songFile = await _songFileProvider.GetSongFileAsync(song);

            var uploadedFileMessage = await _client.SendAudio(
                _storageChatId,
                InputFile.FromStream(songFile.Stream, songFile.FileName),
                title: song.Attributes.Name,
                performer: song.Attributes.ArtistName,
                duration: (int)song.Attributes.Duration.TotalSeconds,
                cancellationToken: cancellationToken);
            var uploadedFile = uploadedFileMessage.Audio!;

            cachedSong = new CachedSong()
            {
                SongId = song.Id,
                Store = store,
                Name = song.Attributes.Name,
                ArtistName = song.Attributes.ArtistName,
                Duration = song.Attributes.Duration,
                FileId = uploadedFile.FileId,
                FileUniqueId = uploadedFile.FileUniqueId,
            };

            await _appDbContext.AddAsync(cachedSong, cancellationToken);
            await _appDbContext.SaveChangesAsync(cancellationToken);
        }

        return cachedSong;
    }
}