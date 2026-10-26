namespace QuizMaster.Api.Models;

public class Document
{
    public Guid Id { get; set; }

    public required string OwnerId { get; set; }

    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }

    public required string StoredFileName { get; set; }

    public DocumentStatus Status { get; set; }
    public string? Error { get; set; }
    public int ChunkCount { get; set; }

    public DateTime UploadedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public List<DocumentChunk> Chunks { get; set; } = [];
}
