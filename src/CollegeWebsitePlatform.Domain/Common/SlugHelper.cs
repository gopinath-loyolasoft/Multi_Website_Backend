namespace CollegeWebsitePlatform.Domain.Common;

using System.Text.RegularExpressions;

public static class SlugHelper
{
    /// <summary>
    /// Converts any text string into a clean, lowercase, URL-safe slug.
    /// e.g. "Computer Science & Engineering!" -> "computer-science-engineering"
    /// </summary>
    public static string Slugify(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var slug = text.ToLowerInvariant().Trim();

        // Replace ampersand with 'and'
        slug = slug.Replace("&", "and");

        // Remove invalid non-alphanumeric characters (except spaces and hyphens)
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");

        // Convert multiple spaces/hyphens/underscores into a single hyphen
        slug = Regex.Replace(slug, @"[\s_-]+", "-");

        // Trim leading and trailing hyphens
        slug = slug.Trim('-');

        return slug;
    }
}
