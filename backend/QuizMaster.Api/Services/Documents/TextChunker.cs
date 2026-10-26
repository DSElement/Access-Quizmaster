using System.Text;
using System.Text.RegularExpressions;

namespace QuizMaster.Api.Services.Documents;

// Splits text into chunks of at most maxChars that overlap by up to overlapChars.
// Chunks end at a paragraph or sentence end where possible, and the overlap is made of whole sentences.
public static partial class TextChunker
{
    private readonly record struct Sentence(string Text, bool StartsParagraph);

    public static List<string> Split(string text, int maxChars, int overlapChars)
    {
        var chunks = new List<string>();
        var current = new List<Sentence>();
        // Sentences in `current` that weren't carried over from the previous chunk.
        var newSentences = 0;

        foreach (var paragraph in Paragraphs(text))
        {
            // Close a half-full chunk at the paragraph break instead of cutting the next paragraph in two.
            if (newSentences > 0 &&
                Length(current) >= maxChars / 2 &&
                Length(current) + 2 + paragraph.Length > maxChars)
            {
                Flush();
            }

            var sentences = Sentences(paragraph, maxChars);
            for (var i = 0; i < sentences.Count; i++)
            {
                var sentence = new Sentence(sentences[i], StartsParagraph: i == 0);

                if (!Fits(sentence) && newSentences > 0)
                    Flush();

                // The overlap plus this sentence can still be too long: drop overlap from the front.
                while (!Fits(sentence))
                    current.RemoveAt(0);

                current.Add(sentence);
                newSentences++;
            }
        }

        if (newSentences > 0)
            chunks.Add(Join(current));

        return chunks;

        bool Fits(Sentence sentence) =>
            current.Count == 0 ||
            Length(current) + Separator(sentence).Length + sentence.Text.Length <= maxChars;

        void Flush()
        {
            chunks.Add(Join(current));

            var overlap = new List<Sentence>();
            var overlapLength = 0;
            for (var i = current.Count - 1; i >= 1; i--)
            {
                overlapLength += current[i].Text.Length + 1;
                if (overlapLength > overlapChars) break;
                overlap.Insert(0, current[i]);
            }

            current = overlap;
            newSentences = 0;
        }
    }

    private static IEnumerable<string> Paragraphs(string text)
    {
        var paragraphs = BlankLine().Split(text)
            .Select(paragraph => string.Join('\n', paragraph
                .Split('\n')
                .Select(line => Spaces().Replace(line, " ").Trim())
                .Where(line => line.Length > 0)))
            .Where(paragraph => paragraph.Length > 0);

        // A heading is glued to the paragraph after it, so it never ends a chunk on its own.
        // The length cap stops a long run of short lines (a list, a table of contents) from becoming one block.
        string? headings = null;
        foreach (var paragraph in paragraphs)
        {
            var combined = headings is null ? paragraph : $"{headings}\n\n{paragraph}";
            if (LooksLikeHeading(paragraph) && combined.Length <= 300)
            {
                headings = combined;
                continue;
            }

            headings = null;
            yield return combined;
        }

        if (headings is not null)
            yield return headings;
    }

    // A Markdown heading, or a short line without end punctuation (how headings look in text extracted from a PDF).
    private static bool LooksLikeHeading(string paragraph) =>
        paragraph.StartsWith('#') ||
        (paragraph.Length <= 80 && !paragraph.Contains('\n') && paragraph[^1] is not ('.' or '!' or '?'));

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

    // Fallback for a "sentence" longer than a whole chunk, like a table or a list without punctuation.
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

    private static string Separator(Sentence sentence) => sentence.StartsParagraph ? "\n\n" : " ";

    private static int Length(List<Sentence> sentences) => Join(sentences).Length;

    private static string Join(List<Sentence> sentences)
    {
        var builder = new StringBuilder();
        foreach (var sentence in sentences)
        {
            if (builder.Length > 0) builder.Append(Separator(sentence));
            builder.Append(sentence.Text);
        }
        return builder.ToString();
    }

    [GeneratedRegex(@"\n[ \t]*\n\s*")]
    private static partial Regex BlankLine();

    [GeneratedRegex(@"[ \t ]+")]
    private static partial Regex Spaces();

    // A sentence ends at . ! ? or … followed by whitespace and a capital letter, digit, quote or bracket.
    // "e.g. this" stays together; "Dr. Smith" gets split, which is harmless for retrieval.
    [GeneratedRegex(@"(?<=[.!?…])\s+(?=[\p{Lu}\p{N}""'“„‘(\[])")]
    private static partial Regex SentenceEnd();
}
