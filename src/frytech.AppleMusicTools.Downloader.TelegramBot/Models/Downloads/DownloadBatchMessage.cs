using Telegram.Bot.Types.ReplyMarkups;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Models.Downloads;

public sealed record DownloadBatchMessage(string Text, InlineKeyboardMarkup ReplyMarkup);
