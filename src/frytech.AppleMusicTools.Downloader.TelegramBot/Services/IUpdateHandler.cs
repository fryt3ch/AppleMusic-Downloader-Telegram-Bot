using Telegram.Bot.Types;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services;

public interface IUpdateHandler
{
    public Task HandleUpdateAsync(Update update, CancellationToken cancellationToken);
}