using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Models.Downloads;

public sealed record DownloadBatchNotice(string Text, InlineKeyboardMarkup ReplyMarkup, ReplyParameters? ReplyParameters, int? ReplaceMessageId);
