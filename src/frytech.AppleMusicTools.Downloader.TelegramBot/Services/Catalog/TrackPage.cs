using frytech.AppleMusic.API.Models.Core;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Catalog;

public sealed record TrackPage(IReadOnlyList<Resource> Items, bool HasMore);
