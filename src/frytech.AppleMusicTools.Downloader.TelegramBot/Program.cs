using frytech.AppleMusicTools.Downloader.TelegramBot.Extensions;
using frytech.AppleMusicTools.Downloader.TelegramBot.Telegram;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAppConfiguration(builder.Configuration)
    .AddPersistence(builder.Configuration)
    .AddAppleMusicCatalog()
    .AddTelegramBot()
    .AddApplicationServices();

var app = builder.Build();

await app.InitializeAsync();
app.UseTelegramBot();
app.Run();
