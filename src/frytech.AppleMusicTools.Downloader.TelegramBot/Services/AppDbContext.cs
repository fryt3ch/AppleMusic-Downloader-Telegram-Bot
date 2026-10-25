using frytech.AppleMusicTools.Downloader.TelegramBot.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services;

public class AppDbContext : DbContext
{
    public DbSet<CachedSong> CachedSongs { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CachedSong>(entity =>
        {
            entity
                .ToTable("CachedSongs")
                .HasKey(x => x.Id);

            entity.HasIndex(x => new { x.NormalizedArtistName, x.NormalizedName });
            entity.HasIndex(x => x.SourceUrl);
        });
    }
}
