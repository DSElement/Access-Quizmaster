using QuizMaster.Api.Models;

namespace QuizMaster.Api.Contracts;

public record DocumentSummary(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    DocumentStatus Status,
    string? Error,
    int ChunkCount,
    DateTime UploadedAt,
    DateTime? ProcessedAt);

public record DocumentChunkView(Guid Id, int Index, string Text);
