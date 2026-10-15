using System.Text;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models.Database;
using KeyedSemaphores;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services;

public class SongSender
{
    private readonly ITelegramBotClient _client;
    private readonly ISongCacher _songCacher;
    private readonly ILogger<SongSender> _logger;

    public SongSender(ITelegramBotClient client, ISongCacher songCacher, ILogger<SongSender> logger)
    {
        _client = client;
        _songCacher = songCacher;
        _logger = logger;
    }
    
    public async Task SendSongs(Chat chat, IEnumerable<string> songIds, string store, ReplyParameters? replyParameters, CancellationToken cancellationToken)
    {
        var songIdsArray = songIds.ToArray();

        Message? statusMessage = null;

        var cachedSongs = new List<CachedSong>();
        var failedSongs = new List<SongDownloadFailedException>();

        string statusMessageText;
        var statusMessageTextWarning = "This process may take a long time so be patient. Only one pending request is available per user, so wait for this one to be done before making a new one.";

        for (var i = 0; i < songIdsArray.Length; i++)
        {
            var songId = songIdsArray[i];

            statusMessageText = $"Songs gathered: {i} of {songIdsArray.Length}.\n\n{statusMessageTextWarning}";

            if (statusMessage is null)
                statusMessage = await _client.SendMessage(chat.Id, statusMessageText, replyParameters: replyParameters, disableNotification: true, cancellationToken: cancellationToken);
            else
                statusMessage = await _client.EditMessageText(chat.Id, statusMessage.MessageId, statusMessageText, cancellationToken: cancellationToken);

            using (await KeyedSemaphore.LockAsync(songId, cancellationToken))
            {
                try
                {
                    var cachedSong = await _songCacher.GetCachedSong(songId, store, cancellationToken);

                    cachedSongs.Add(cachedSong);
                }
                catch (SongDownloadFailedException exception)
                {
                    _logger.LogWarning(exception, "Skipping song {SongId} because it could not be downloaded.", songId);

                    failedSongs.Add(exception);
                }
            }
        }

        var batchSize = 10;
        
        var batches = cachedSongs
            .Select((dataElement, index) => new { dataElement, index })
            .GroupBy(x => x.index / batchSize, x => x.dataElement)
            .ToArray();

        var sentSongsAmount = 0;
        
        foreach (var batch in batches)
        {
            var batchArray = batch.ToArray();

            if (statusMessage is not null && batches.Length > 1)
            {
                statusMessageText = $"All songs were gathered, sending them in groups: {sentSongsAmount} of {songIdsArray.Length}.\n\n{statusMessageTextWarning}";
                
                await _client.EditMessageText(chat.Id, statusMessage.MessageId, statusMessageText, cancellationToken: cancellationToken);
            }

            await _client.SendMediaGroup(
                chat.Id,
                batchArray.Select(CreateInputMediaAudio),
                disableNotification: true,
                replyParameters: replyParameters,
                cancellationToken: cancellationToken);
            
            sentSongsAmount += batchArray.Length;
        }
        
        if (statusMessage is not null)
            await _client.DeleteMessage(chat.Id, statusMessage.MessageId, cancellationToken: cancellationToken);

        if (failedSongs.Count > 0)
            await SendFailedSongsReport(chat, failedSongs, replyParameters, cancellationToken);
    }

    private async Task SendFailedSongsReport(Chat chat, IReadOnlyCollection<SongDownloadFailedException> failedSongs, ReplyParameters? replyParameters, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Couldn't get {failedSongs.Count} song(s):");

        foreach (var failedSong in failedSongs)
            builder.AppendLine($"• {failedSong.ArtistName} — {failedSong.Name}");

        await _client.SendMessage(
            chat.Id,
            builder.ToString(),
            replyParameters: replyParameters,
            disableNotification: true,
            cancellationToken: cancellationToken);
    }

    private static InputMediaAudio CreateInputMediaAudio(CachedSong cachedSong)
    {
        return new InputMediaAudio(InputFile.FromFileId(cachedSong.FileId))
        {
            Title = cachedSong.Name,
            Performer = cachedSong.ArtistName,
            Duration = (int)cachedSong.Duration.TotalSeconds,
        };
    }
}