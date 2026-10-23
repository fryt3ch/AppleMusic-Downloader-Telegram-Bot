using Telegram.Bot.Types;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Requests;

public interface IUpdateHandler
{
    public Task HandleUpdateAsync(Update update, CancellationToken cancellationToken);
}