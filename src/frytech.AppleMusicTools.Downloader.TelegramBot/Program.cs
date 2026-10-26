using System.Runtime.InteropServices;
using frytech.AppleMusicTools.Downloader.TelegramBot.Extensions;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Seeding;
using frytech.AppleMusicTools.Downloader.TelegramBot.Telegram;

var normalizedArguments = NormalizeArguments(args);
var builder = WebApplication.CreateBuilder(normalizedArguments);
var seedMode = args.Any(argument => string.Equals(argument, "--seed", StringComparison.OrdinalIgnoreCase));

ApplySeedArgumentOverrides(builder.Configuration, normalizedArguments);

if (seedMode)
    builder.Configuration["SEED:ENABLED"] = "true";

builder.Services
    .AddAppConfiguration(builder.Configuration)
    .AddPersistence(builder.Configuration)
    .AddAppleMusicCatalog()
    .AddTelegramBot()
    .AddApplicationServices();

var app = builder.Build();

await app.InitializeAsync();

if (seedMode)
{
    using var cancellation = new CancellationTokenSource();
    using var sigint = TryRegisterSignal(PosixSignal.SIGINT, cancellation);
    using var sigterm = TryRegisterSignal(PosixSignal.SIGTERM, cancellation);

    try
    {
        await app.Services.GetRequiredService<SeedRunner>().RunAsync(cancellation.Token);
    }
    catch (OperationCanceledException)
    {
        app.Logger.LogInformation("Seed was cancelled");
    }

    return;
}

app.UseTelegramBot();
app.Run();

static IDisposable? TryRegisterSignal(PosixSignal signal, CancellationTokenSource cancellation)
{
    try
    {
        return PosixSignalRegistration.Create(signal, context =>
        {
            context.Cancel = true;
            cancellation.Cancel();
        });
    }
    catch (Exception)
    {
        return null;
    }
}

static void ApplySeedArgumentOverrides(ConfigurationManager configuration, string[] arguments)
{
    var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    foreach (var argument in arguments)
    {
        if (!argument.StartsWith("--SEED:", StringComparison.OrdinalIgnoreCase))
            continue;

        var separator = argument.IndexOf('=');

        if (separator <= "--SEED:".Length)
            continue;

        var key = argument[2..separator];
        var value = argument[(separator + 1)..];

        overrides[key] = value;
    }

    if (overrides.Count > 0)
        configuration.AddInMemoryCollection(overrides);
}

static string[] NormalizeArguments(string[] arguments)
{
    var normalized = new string[arguments.Length];

    for (var index = 0; index < arguments.Length; index++)
    {
        var argument = arguments[index];

        if (!argument.StartsWith("--", StringComparison.Ordinal))
        {
            normalized[index] = argument;

            continue;
        }

        var separator = argument.IndexOf('=');

        if (separator < 0)
        {
            normalized[index] = argument;

            continue;
        }

        var key = argument[..separator].Replace("__", ":", StringComparison.Ordinal);

        normalized[index] = key + argument[separator..];
    }

    return normalized;
}
