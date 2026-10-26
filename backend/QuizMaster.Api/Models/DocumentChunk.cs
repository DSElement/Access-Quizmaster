using Pgvector;

namespace QuizMaster.Api.Models;

public class DocumentChunk
{
    public const int EmbeddingDimensions = 768;

    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }

    public int Index { get; set; }

    public required string Text { get; set; }

    public required Vector Embedding { get; set; }
}
