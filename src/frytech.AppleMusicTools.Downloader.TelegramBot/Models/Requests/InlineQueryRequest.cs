namespace frytech.AppleMusicTools.Downloader.TelegramBot.Models.Requests;

public sealed record InlineQueryRequest(string Payload, QueryType QueryType, int Offset);
