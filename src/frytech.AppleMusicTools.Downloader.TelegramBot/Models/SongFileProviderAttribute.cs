namespace frytech.AppleMusicTools.Downloader.TelegramBot.Models;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SongFileProviderAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
