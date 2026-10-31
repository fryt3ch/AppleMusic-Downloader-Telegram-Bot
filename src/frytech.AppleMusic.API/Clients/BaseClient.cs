using System.Collections.Specialized;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using frytech.AppleMusic.API.Configuration;
using frytech.AppleMusic.API.Converters;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Extensions;
using Microsoft.Extensions.Options;

namespace frytech.AppleMusic.API.Clients;

/// <summary>
/// Base client.
/// </summary>
public abstract class BaseClient
{
    private const string JsonMediaType = "application/json";
    private const string UserTokenHeaderName = "Media-User-Token";
    private const string LimitQueryStringKey = "limit";
    private const string OffsetQueryStringKey = "offset";
    private const string LocaleQueryStringKey = "l";

    protected HttpClient Client { get; }
        
    protected JsonSerializerOptions JsonOptions { get; }

    protected BaseClient(HttpClient client, IOptions<AppleMusicClientConfiguration> options)
    {
        Client = client ?? throw new ArgumentNullException(nameof(client));

        Client.BaseAddress = new Uri(options.Value.BaseUrl);

        Client.DefaultRequestHeaders.Accept.Clear();
        Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(JsonMediaType));
        
        Client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Macintosh; U; PPC Mac OS X; it-it) AppleWebKit/419 (KHTML, like Gecko) Safari/419.3");
        Client.DefaultRequestHeaders.Add("Origin", "https://music.apple.com");
        
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.Jwt);

        JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters =
            {
                new DateTimeConverter(),
                new TimeSpanMillisecondsConverter(),
                new JsonStringEnumConverter(),
            },
            AllowOutOfOrderMetadataProperties = true,
        };
    }

    protected void SetUserTokenHeader(string userToken)
    {
        Client.DefaultRequestHeaders.Add(UserTokenHeaderName, userToken);
    }

    /// <summary>
    /// Send a GET request to the api.
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="requestUri"></param>
    /// <param name="queryStringParameters"></param>
    /// <param name="pageOptions"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    protected async Task<TResponse> Get<TResponse>(string requestUri, NameValueCollection? queryStringParameters = null, PageOptions? pageOptions = null, string? locale = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestUri, nameof(requestUri));

        queryStringParameters ??= new NameValueCollection();

        if (pageOptions?.Limit is not null)
            queryStringParameters.Add(LimitQueryStringKey, pageOptions.Limit.Value.ToString());

        if (pageOptions?.Offset is not  null)
            queryStringParameters.Add(OffsetQueryStringKey, pageOptions.Offset.Value.ToString());

        if (locale != null)
            queryStringParameters.Add(LocaleQueryStringKey, locale);

        var queryString = queryStringParameters.ToQueryString();

        if (!string.IsNullOrWhiteSpace(queryString))
            requestUri += $"?{queryString}";

        var response = await Client.GetAsync(requestUri)
            .ConfigureAwait(false);

        var str = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        var t = typeof(Resource).GetCustomAttributes(false);

        return JsonSerializer.Deserialize<TResponse>(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), JsonOptions)!;
    }

    /// <summary>
    /// Send a PUT request to the api.
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    /// <typeparam name="TRequest"></typeparam>
    /// <param name="requestUri"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    protected async Task<TResponse> Put<TResponse, TRequest>(string requestUri, TRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestUri, nameof(requestUri));
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var json = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, JsonMediaType);
            
        var response = await Client.PutAsync(requestUri, content).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<TResponse>(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), JsonOptions)!;
    }

    /// <summary>
    /// Send a POST request with no body to the api.
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="requestUri"></param>
    /// <param name="queryStringParameters"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    protected async Task<TResponse> Post<TResponse>(string requestUri, NameValueCollection? queryStringParameters = null, string? locale = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestUri, nameof(requestUri));

        queryStringParameters ??= new NameValueCollection();

        if (locale != null)
            queryStringParameters.Add(LocaleQueryStringKey, locale);

        var queryString = queryStringParameters.ToQueryString();

        if (!string.IsNullOrWhiteSpace(queryString))
            requestUri += $"?{queryString}";

        var response = await Client.PostAsync(requestUri, null).ConfigureAwait(false);

        return JsonSerializer.Deserialize<TResponse>(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), JsonOptions)!;
    }

    /// <summary>
    /// Send a POST request to the api.
    /// </summary>
    /// <param name="requestUri"></param>
    /// <param name="request"></param>
    /// <param name="queryStringParameters"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    protected async Task<TResponse> Post<TResponse, TRequest>(string requestUri, TRequest request, NameValueCollection? queryStringParameters = null, string? locale = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestUri, nameof(requestUri));

        queryStringParameters ??= new NameValueCollection();

        if (locale is not null)
            queryStringParameters.Add(LocaleQueryStringKey, locale);

        var queryString = queryStringParameters.ToQueryString();

        if (!string.IsNullOrWhiteSpace(queryString))
            requestUri += $"?{queryString}";

        var json = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, JsonMediaType);
            
        var response = await Client.PostAsync(requestUri, content).ConfigureAwait(false);

        return JsonSerializer.Deserialize<TResponse>(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), JsonOptions)!;
    }

    /// <summary>
    /// Send a DELETE request to the api.
    /// </summary>
    /// <param name="requestUri"></param>
    /// <returns></returns>
    protected async Task<ResponseRoot> Delete(string requestUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestUri, nameof(requestUri));

        var response = await Client.DeleteAsync(requestUri).ConfigureAwait(false);

        return JsonSerializer.Deserialize<ResponseRoot>(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), JsonOptions)!;
    }
}