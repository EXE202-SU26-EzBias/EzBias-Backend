using System.Text;

namespace EzBias.Domain.Services;

public static class FandomNameNormalizer
{
    public const int MaxLength = 100;

    public static bool TryNormalize(
        string? value,
        out string displayName,
        out string normalizedName,
        out string? error)
    {
        displayName = string.Empty;
        normalizedName = string.Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            error = "Fandom is required.";
            return false;
        }

        try
        {
            displayName = CollapseWhitespace(value.Normalize(NormalizationForm.FormKC));
        }
        catch (ArgumentException)
        {
            error = "Fandom name contains invalid characters.";
            return false;
        }

        if (displayName.Length == 0)
        {
            error = "Fandom is required.";
            return false;
        }

        if (displayName.Length > MaxLength)
        {
            error = $"Fandom name must be {MaxLength} characters or fewer.";
            return false;
        }

        normalizedName = displayName.ToLowerInvariant();
        return true;
    }

    public static string ToSlug(string displayName)
    {
        var builder = new StringBuilder(displayName.Length);
        var pendingSeparator = false;

        foreach (var character in displayName.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                if (pendingSeparator && builder.Length > 0)
                    builder.Append('-');

                builder.Append(character);
                pendingSeparator = false;
            }
            else
            {
                pendingSeparator = builder.Length > 0;
            }
        }

        return builder.Length == 0 ? "fandom" : builder.ToString();
    }

    private static string CollapseWhitespace(string value)
    {
        var builder = new StringBuilder(value.Length);
        var pendingSpace = false;

        foreach (var character in value)
        {
            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
                builder.Append(' ');

            builder.Append(character);
            pendingSpace = false;
        }

        return builder.ToString();
    }
}
