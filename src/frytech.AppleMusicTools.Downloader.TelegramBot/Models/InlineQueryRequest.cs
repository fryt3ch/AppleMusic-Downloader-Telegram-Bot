namespace frytech.AppleMusicTools.Downloader.TelegramBot.Models;

public sealed record InlineQueryRequest(string Payload, QueryType QueryType, int Offset);
