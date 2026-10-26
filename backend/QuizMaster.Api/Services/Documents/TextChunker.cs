using System.Text;
using System.Text.RegularExpressions;

namespace QuizMaster.Api.Services.Documents;

// Splits text into chunks of at most maxChars that overlap by up to overlapChars.
// Chunks end at a paragraph or sentence end where possible, and the overlap is made of whole sentences.
// Indentation is kept, and fenced code blocks (``` or ~~~) are cut only between lines.
public static partial class TextChunker
{
    private readonly record struct Paragraph(string Text, bool IsCode);

    // A sentence of prose or a line of code. Separator is what goes before it when it isn't first in a chunk.
    private readonly record struct Piece(string Text, string Separator);

    public static List<string> Split(string text, int maxChars, int overlapChars)
    {
        var chunks = new List<string>();
        var current = new List<Piece>();
        // Pieces in `current` that weren't carried over from the previous chunk.
        var newPieces = 0;

        foreach (var paragraph in Paragraphs(text))
        {
            // Close a half-full chunk at the paragraph break instead of cutting the next paragraph in two.
            if (newPieces > 0 &&
                Length(current) >= maxChars / 2 &&
                Length(current) + 2 + paragraph.Text.Length > maxChars)
            {
                Flush();
            }

            foreach (var piece in Pieces(paragraph, maxChars))
            {
                if (!Fits(piece) && newPieces > 0)
                    Flush();

                // The overlap plus this piece can still be too long: drop overlap from the front.
                while (!Fits(piece))
                    current.RemoveAt(0);

                current.Add(piece);
                newPieces++;
            }
        }

        if (newPieces > 0)
            chunks.Add(Join(current).Trim('\n'));

        return chunks;

        bool Fits(Piece piece) =>
            current.Count == 0 ||
            Length(current) + piece.Separator.Length + piece.Text.Length <= maxChars;

        void Flush()
        {
            // A blank line of code can land at either end of a chunk; drop it, but keep indentation.
            chunks.Add(Join(current).Trim('\n'));

            var overlap = new List<Piece>();
            var overlapLength = 0;
            for (var i = current.Count - 1; i >= 1; i--)
            {
                overlapLength += current[i].Text.Length + 1;
                if (overlapLength > overlapChars) break;
                overlap.Insert(0, current[i]);
            }

            current = overlap;
            newPieces = 0;
        }
    }

    private static IEnumerable<Paragraph> Paragraphs(string text)
    {
        // A heading is glued to the paragraph after it, so it never ends a chunk on its own.
        // The length cap stops a long run of short lines (a list, a table of contents) from becoming one block.
        string? headings = null;
        foreach (var block in Blocks(text))
        {
            var combined = headings is null ? block.Text : $"{headings}\n\n{block.Text}";
            if (!block.IsCode && LooksLikeHeading(block.Text) && combined.Length <= 300)
            {
                headings = combined;
                continue;
            }

            headings = null;
            yield return block with { Text = combined };
        }

        if (headings is not null)
            yield return new Paragraph(headings, IsCode: false);
    }

    // Paragraphs are separated by blank lines; a fenced code block is one paragraph even if it contains blank lines.
    private static List<Paragraph> Blocks(string text)
    {
        var blocks = new List<Paragraph>();
        var lines = new List<string>();
        string? fence = null;

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Replace("\t", "    ").TrimEnd();
            var trimmed = line.TrimStart();

            if (fence is not null)
            {
                lines.Add(line);
                if (trimmed.StartsWith(fence, StringComparison.Ordinal))
                {
                    blocks.Add(new Paragraph(string.Join('\n', lines), IsCode: true));
                    lines.Clear();
                    fence = null;
                }
                continue;
            }

            if (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal))
            {
                AddProse();
                fence = trimmed[..3];
                lines.Add(line);
                continue;
            }

            if (trimmed.Length == 0)
            {
                AddProse();
                continue;
            }

            // Keep the indentation, but collapse runs of spaces inside the line.
            var indentation = line[..(line.Length - trimmed.Length)];
            lines.Add(indentation + Spaces().Replace(trimmed, " "));
        }

        if (fence is not null)
            blocks.Add(new Paragraph(string.Join('\n', lines), IsCode: true));
        else
            AddProse();

        return blocks;

        void AddProse()
        {
            if (lines.Count == 0) return;
            blocks.Add(new Paragraph(string.Join('\n', lines), IsCode: false));
            lines.Clear();
        }
    }

    // A Markdown heading, or a short line without end punctuation (how headings look in text extracted from a PDF).
    private static bool LooksLikeHeading(string paragraph)
    {
        var text = paragraph.TrimStart();
        return text.StartsWith('#') ||
            (text.Length <= 80 && !text.Contains('\n') && text[^1] is not ('.' or '!' or '?'));
    }

    private static IEnumerable<Piece> Pieces(Paragraph paragraph, int maxChars)
    {
        var texts = paragraph.IsCode ? CodeLines(paragraph.Text, maxChars) : Sentences(paragraph.Text, maxChars);
        var continuation = paragraph.IsCode ? "\n" : " ";
        return texts.Select((text, index) => new Piece(text, index == 0 ? "\n\n" : continuation));
    }

    private static List<string> Sentences(string paragraph, int maxChars)
    {
        var sentences = new List<string>();
        foreach (var sentence in SentenceEnd().Split(paragraph))
        {
            if (sentence.Length <= maxChars)
                sentences.Add(sentence);
            else
                sentences.AddRange(SplitByWords(sentence, maxChars));
        }
        return sentences;
    }

    // Each line of code is its own piece, so code is cut between lines and the overlap repeats whole lines.
    private static IEnumerable<string> CodeLines(string code, int maxChars) =>
        code.Split('\n').SelectMany(line => line.Length <= maxChars ? [line] : SplitByWords(line, maxChars));

    // Fallback for a "sentence" or line longer than a whole chunk, like a table or a list without punctuation.
    private static IEnumerable<string> SplitByWords(string text, int maxChars)
    {
        var piece = new StringBuilder();
        foreach (var word in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (piece.Length > 0 && piece.Length + 1 + word.Length > maxChars)
            {
                yield return piece.ToString();
                piece.Clear();
            }

            if (word.Length > maxChars)
            {
                foreach (var part in word.Chunk(maxChars))
                    yield return new string(part);
                continue;
            }

            if (piece.Length > 0) piece.Append(' ');
            piece.Append(word);
        }

        if (piece.Length > 0)
            yield return piece.ToString();
    }

    private static int Length(List<Piece> pieces) => Join(pieces).Length;

    private static string Join(List<Piece> pieces)
    {
        var builder = new StringBuilder();
        foreach (var piece in pieces)
        {
            if (builder.Length > 0) builder.Append(piece.Separator);
            builder.Append(piece.Text);
        }
        return builder.ToString();
    }

    [GeneratedRegex(@"[ \t ]+")]
    private static partial Regex Spaces();

    // A sentence ends at . ! ? or … followed by whitespace and a capital letter, digit, quote or bracket.
    // "e.g. this" stays together; "Dr. Smith" gets split, which is harmless for retrieval.
    [GeneratedRegex(@"(?<=[.!?…])\s+(?=[\p{Lu}\p{N}""'“„‘(\[])")]
    private static partial Regex SentenceEnd();
}
