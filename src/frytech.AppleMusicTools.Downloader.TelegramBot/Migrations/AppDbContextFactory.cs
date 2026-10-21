using frytech.AppleMusicTools.Downloader.TelegramBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Migrations;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=data/app.db")
            .Options;

        return new AppDbContext(options);
    }
}
