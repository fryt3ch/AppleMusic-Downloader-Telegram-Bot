using frytech.AppleMusic.API.Models.Enums;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Models;

public sealed record MusicElementReference(ResourceType ResourceType, string Id);
