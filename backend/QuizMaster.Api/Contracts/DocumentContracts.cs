using System.ComponentModel.DataAnnotations;
using QuizMaster.Api.Models;
using QuizMaster.Api.Services.Documents;

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
    DateTime? ProcessedAt,
    IndexingStep? Progress);

public record DocumentChunkView(Guid Id, int Index, string Text);

public record RenameDocumentRequest(
    [Required, StringLength(255)]
    string Name);
