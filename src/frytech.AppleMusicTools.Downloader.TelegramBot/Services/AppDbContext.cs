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
        modelBuilder.Entity<CachedSong>()
            .ToTable("CachedSongs")
            .HasKey(x => new { x.SongId, x.Store });
    }
}