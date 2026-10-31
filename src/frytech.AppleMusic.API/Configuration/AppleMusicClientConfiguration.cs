namespace frytech.AppleMusic.API.Configuration;

public class AppleMusicClientConfiguration
{
    public string BaseUrl { get; set; } = "https://api.music.apple.com/v1/";
    
    public required string Jwt { get; set; }
}