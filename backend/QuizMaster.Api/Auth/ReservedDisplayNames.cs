using System.Globalization;
using System.Text;

namespace QuizMaster.Api.Auth;

// Display names that could pass for the admin account, like "Admin", "ADMIN 2", "4dm1n", "a.d.m.i.n" or "Ａｄｍｉｎ".
public static class ReservedDisplayNames
{
    public const string Message = "Display names can't contain \"admin\".";

    public static bool IsReserved(string displayName) => Fold(displayName).Contains("admin");

    // Reduces a name to plain lowercase letters, so look-alike spellings compare equal.
    private static string Fold(string value)
    {
        var letters = new StringBuilder(value.Length);

        // FormKD splits accents off letters (á -> a + accent) and turns full-width or styled letters into plain ones.
        foreach (var c in value.Normalize(NormalizationForm.FormKD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;

            var letter = char.ToLowerInvariant(c) switch
            {
                '4' or '@' or 'а' => 'a',          // the last one is Cyrillic
                '1' or '!' or '|' or 'l' or 'і' => 'i',
                '0' => 'o',
                var other => other,
            };

            if (char.IsLetter(letter))
                letters.Append(letter);
        }

        // "rn" next to each other looks like "m".
        return letters.ToString().Replace("rn", "m");
    }
}
