using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace QuizMaster.Api.Services.Documents;

// IsTooLong means reading stopped after maxChars, so Text is only the beginning of the document.
public record ExtractedText(string Text, bool IsTooLong);

public static partial class TextExtractor
{
    // Stops reading after maxChars, so a small file that unpacks into a huge amount of text
    // (a compressed PDF can) doesn't tie up the worker or fill the memory.
    public static async Task<ExtractedText> ExtractAsync(
        string path, string contentType, int maxChars, CancellationToken cancellationToken)
    {
        var raw = contentType == DocumentFiles.Pdf
            ? ExtractPdf(path, maxChars, cancellationToken)
            : await ReadTextAsync(path, maxChars, cancellationToken);

        var isTooLong = raw.Length > maxChars;
        var text = Clean(raw);

        // Single line breaks in a PDF only show where the page was too narrow, so they become spaces.
        if (contentType == DocumentFiles.Pdf)
            text = LineWrap().Replace(text, " ");

        return new ExtractedText(text, isTooLong);
    }

    // Reads at most maxChars + 1 characters: one more than allowed is enough to know the text is too long.
    private static async Task<string> ReadTextAsync(string path, int maxChars, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(path);
        var buffer = new char[maxChars + 1];
        var read = await reader.ReadBlockAsync(buffer.AsMemory(), cancellationToken);
        return new string(buffer, 0, read);
    }

    // Reads the text layer only: a scanned PDF (pictures of pages) has none and comes back empty.
    private static string ExtractPdf(string path, int maxChars, CancellationToken cancellationToken)
    {
        var options = new ContentOrderTextExtractor.Options { SeparateParagraphsWithDoubleNewline = true };
        var text = new StringBuilder();

        using var pdf = PdfDocument.Open(path);
        foreach (var page in pdf.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (text.Length > 0) text.Append("\n\n");
            text.Append(ContentOrderTextExtractor.GetText(page, options));

            if (text.Length > maxChars) break;
        }

        return text.ToString();
    }

    // Unifies line endings and drops control characters (PostgreSQL text can't even store \0).
    private static string Clean(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var c in text.Replace("\r\n", "\n").Replace('\r', '\n'))
        {
            if (c == '\n' || c == '\t' || !char.IsControl(c))
                builder.Append(c);
        }
        return builder.ToString();
    }

    [GeneratedRegex(@"(?<!\n)\n(?!\n)")]
    private static partial Regex LineWrap();
}
