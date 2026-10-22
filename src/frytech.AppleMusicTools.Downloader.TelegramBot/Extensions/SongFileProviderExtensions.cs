using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.SongFiles;
using Microsoft.Extensions.Options;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Extensions;

public static class SongFileProviderExtensions
{
    public static IServiceCollection AddSongFileProviders(this IServiceCollection services)
    {
        services.AddScoped<PythonProcessRunner>();

        foreach (var registration in SongFileProviderRegistry.All)
            services.AddKeyedScoped(typeof(ISongFileProvider), registration.Name, registration.Type);

        services.AddScoped<ISongFileProvider>(serviceProvider =>
        {
            var configured = serviceProvider
                .GetRequiredService<IOptions<AppSettings>>()
                .Value.Downloader.Provider;

            var key = configured.ToLowerInvariant();

            if (!SongFileProviderRegistry.Names.Contains(key, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Unknown song file provider '{configured}'. Available providers: {string.Join(", ", SongFileProviderRegistry.Names)}.");

            return (ISongFileProvider)serviceProvider.GetRequiredKeyedService(typeof(ISongFileProvider), key);
        });

        return services;
    }
}
