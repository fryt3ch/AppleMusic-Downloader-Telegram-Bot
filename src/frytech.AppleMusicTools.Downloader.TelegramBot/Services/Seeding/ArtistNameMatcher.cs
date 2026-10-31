using System.Globalization;
using System.Text;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Seeding;

public static class ArtistNameMatcher
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var previousWasSpace = true;

        foreach (var character in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);

            if (category is UnicodeCategory.NonSpacingMark)
                continue;

            if (character is '&')
            {
                AppendWord(builder, "and", ref previousWasSpace);

                continue;
            }

            if (character is '\'' or '\u2019' or '!' or '.' or ',' or '?' or ':' or ';' or '$')
                continue;

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                previousWasSpace = false;
            }
            else if (!previousWasSpace)
            {
                builder.Append(' ');
                previousWasSpace = true;
            }
        }

        var normalized = builder.ToString().Trim();

        if (normalized.StartsWith("the ", StringComparison.Ordinal))
            normalized = normalized[4..];

        return normalized;
    }

    public static double Score(string expectedName, string candidateName)
    {
        var expected = Normalize(expectedName);
        var candidate = Normalize(candidateName);

        if (expected.Length == 0 || candidate.Length == 0)
            return 0;

        if (string.Equals(expected, candidate, StringComparison.Ordinal))
            return 1;

        var distance = LevenshteinDistance(expected, candidate);
        var length = Math.Max(expected.Length, candidate.Length);
        var distanceScore = length == 0 ? 0 : 1.0 - (double)distance / length;

        var expectedTokens = expected.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var candidateTokens = candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var tokenScore = TokenOverlap(expectedTokens, candidateTokens);

        return Math.Max(distanceScore, tokenScore);
    }

    private static double TokenOverlap(IReadOnlyCollection<string> expected, IReadOnlyCollection<string> candidate)
    {
        if (expected.Count == 0 || candidate.Count == 0)
            return 0;

        var expectedSet = new HashSet<string>(expected, StringComparer.Ordinal);
        var candidateSet = new HashSet<string>(candidate, StringComparer.Ordinal);

        var intersection = expectedSet.Count(candidateSet.Contains);
        var union = expectedSet.Count + candidateSet.Count - intersection;

        if (union == 0)
            return 0;

        var jaccard = (double)intersection / union;
        var smaller = Math.Min(expectedSet.Count, candidateSet.Count);
        var coverage = (double)intersection / smaller;

        return Math.Max(jaccard, coverage * 0.9);
    }

    private static int LevenshteinDistance(string left, string right)
    {
        if (left.Length == 0)
            return right.Length;

        if (right.Length == 0)
            return left.Length;

        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];

        for (var j = 0; j <= right.Length; j++)
            previous[j] = j;

        for (var i = 1; i <= left.Length; i++)
        {
            current[0] = i;

            for (var j = 1; j <= right.Length; j++)
            {
                var cost = left[i - 1] == right[j - 1] ? 0 : 1;

                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }

    private static void AppendWord(StringBuilder builder, string word, ref bool previousWasSpace)
    {
        if (!previousWasSpace)
            builder.Append(' ');

        builder.Append(word);
        previousWasSpace = false;
    }
}
