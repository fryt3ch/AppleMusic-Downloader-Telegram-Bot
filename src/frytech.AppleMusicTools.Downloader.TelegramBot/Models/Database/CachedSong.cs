using System.ComponentModel.DataAnnotations;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Models.Database;

public sealed class CachedSong
{
    [Key]
    public string SongId { get; set; } = null!;
    
    [Key]
    public string Store { get; set; } = null!;

    [Required]
    public string FileId { get; set; } = null!;

    [Required]
    public string FileUniqueId { get; set; } = null!;
    
    [Required]
    public string Name { get; set; } = null!;
    
    [Required]
    public string ArtistName { get; set; } = null!;
    
    [Required]
    public TimeSpan Duration { get; set; }
}