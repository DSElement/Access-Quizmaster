namespace QuizMaster.Api.Services.Documents;

public static class DocumentFiles
{
    // Constants because [RequestSizeLimit] needs a compile-time value.
    public const long MaxFileBytes = 10 * 1024 * 1024;

    // The multipart form wraps the file in a few headers, so the request is slightly bigger than the file.
    public const long MaxRequestBytes = MaxFileBytes + 64 * 1024;

    public const string Pdf = "application/pdf";

    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = Pdf,
        [".txt"] = "text/plain",
        [".md"] = "text/markdown",
        [".markdown"] = "text/markdown",
    };

    public static string? ContentTypeFor(string fileName) =>
        ContentTypes.GetValueOrDefault(Path.GetExtension(fileName));

    // The extension and the browser's content type are only claims, so look at the first bytes too.
    public static async Task<string?> FindContentProblemAsync(Stream stream, string contentType, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        var read = await stream.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false, cancellationToken);
        var head = buffer.AsSpan(0, read);

        if (contentType == Pdf)
            return head.StartsWith("%PDF-"u8) ? null : "This file isn't a valid PDF.";

        // Text files don't contain NUL bytes; binary files (and UTF-16 text) almost always do.
        return head.Contains((byte)0) ? "This file isn't UTF-8 text." : null;
    }
}
