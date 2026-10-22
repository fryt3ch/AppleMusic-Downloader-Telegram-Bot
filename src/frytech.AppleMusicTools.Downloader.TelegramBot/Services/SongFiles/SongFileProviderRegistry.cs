using System.Reflection;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.SongFiles;

public sealed record SongFileProviderRegistration(string Name, Type Type);

public static class SongFileProviderRegistry
{
    public static IReadOnlyList<SongFileProviderRegistration> All { get; } = Discover();

    public static IReadOnlyList<string> Names { get; } = All.Select(x => x.Name).ToArray();

    private static IReadOnlyList<SongFileProviderRegistration> Discover()
    {
        var registrations = new List<SongFileProviderRegistration>();

        var providerTypes = typeof(ISongFileProvider).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(ISongFileProvider).IsAssignableFrom(type));

        foreach (var providerType in providerTypes)
        {
            var attribute = providerType.GetCustomAttribute<SongFileProviderAttribute>(inherit: false)
                ?? throw new InvalidOperationException($"Song file provider '{providerType.Name}' is missing [{nameof(SongFileProviderAttribute)}].");

            registrations.Add(new SongFileProviderRegistration(attribute.Name, providerType));
        }

        var duplicateName = registrations
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateName is not null)
            throw new InvalidOperationException($"Duplicate song file provider name '{duplicateName.Key}'.");

        return registrations;
    }
}
