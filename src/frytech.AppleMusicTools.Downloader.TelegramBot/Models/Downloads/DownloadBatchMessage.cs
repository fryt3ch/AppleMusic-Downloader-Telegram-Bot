namespace frytech.AppleMusicTools.Downloader.TelegramBot.Models.Downloads;

public sealed record DownloadBatchMessage(string Text, Func<int, string> BuildCallback);
