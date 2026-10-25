using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Caching;

public static partial class SongIdentityNormalizer
{
    public const char MatchKeySeparator = '\u001F';

    public static string BuildMatchKey(string artist, string title)
    {
        return $"{Normalize(artist)}{MatchKeySeparator}{Normalize(title)}";
    }

    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Normalize(NormalizationForm.FormKC).ToLowerInvariant();

        normalized = normalized
            .Replace('\u2018', '\'')
            .Replace('\u2019', '\'')
            .Replace('\u201B', '\'')
            .Replace('\u2013', ' ')
            .Replace('\u2014', ' ');

        normalized = RemoveDiacritics(normalized);
        normalized = normalized.Replace("'", string.Empty);
        normalized = NonAlphaNumericRegex().Replace(normalized, " ");
        normalized = WhitespaceRegex().Replace(normalized, " ").Trim();

        return normalized.Length > 0
            ? normalized
            : value.Normalize(NormalizationForm.FormKC).Trim().ToLowerInvariant();
    }

    private static string RemoveDiacritics(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex(@"[^\p{L}\p{N}]+", RegexOptions.CultureInvariant)]
    private static partial Regex NonAlphaNumericRegex();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();
}
