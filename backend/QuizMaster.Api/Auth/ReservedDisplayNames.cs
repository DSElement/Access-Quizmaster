using System.Globalization;
using System.Text;

namespace QuizMaster.Api.Auth;

public static class ReservedDisplayNames
{
    public const string Message = "Display names can't contain \"admin\".";

    public static bool IsReserved(string displayName) => Fold(displayName).Contains("admin");

    private static string Fold(string value)
    {
        var letters = new StringBuilder(value.Length);

        foreach (var c in value.Normalize(NormalizationForm.FormKD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;

            var letter = char.ToLowerInvariant(c) switch
            {
                '4' or '@' or 'а' => 'a',
                '1' or '!' or '|' or 'l' or 'і' => 'i',
                '0' => 'o',
                var other => other,
            };

            if (char.IsLetter(letter))
                letters.Append(letter);
        }

        return letters.ToString().Replace("rn", "m");
    }
}
