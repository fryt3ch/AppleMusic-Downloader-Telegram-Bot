using frytech.AppleMusic.API.Clients;
using frytech.AppleMusic.API.Clients.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace frytech.AppleMusic.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddAppleMusicCatalogClient(this IServiceCollection services)
    {
        return services.AddHttpClient<ICatalogClient, CatalogClient>();
    }
}