using System.Globalization;
using System.Text;

namespace CTUScheduler.Core.Helpers;

public static class StringHelper
{
    public static string NormalizeString(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        var sb = new StringBuilder(input.Length);

        foreach (var c in input.Normalize(NormalizationForm.FormD))
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c))
            {
                sb.Append(char.ToLowerInvariant(c));
            }
        }
        return sb.ToString();
    }
}
