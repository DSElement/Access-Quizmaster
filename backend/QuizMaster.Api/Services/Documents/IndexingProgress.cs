using System.Collections.Concurrent;

namespace QuizMaster.Api.Services.Documents;

public enum IndexingStage
{
    Reading,
    Embedding,
    RateLimited,
}

// Done and Total count chunks; both are 0 while the document is still being read.
public record IndexingStep(IndexingStage Stage, int Done, int Total);

// Where the worker is with each document it's indexing, for the progress bar.
// Kept in memory: it only matters while the worker is busy, and after a restart unfinished documents start over.
public class IndexingProgress
{
    private readonly ConcurrentDictionary<Guid, IndexingStep> steps = new();

    public void Report(Guid documentId, IndexingStep step) => steps[documentId] = step;

    public IndexingStep? Get(Guid documentId) => steps.GetValueOrDefault(documentId);

    public void Clear(Guid documentId) => steps.TryRemove(documentId, out _);
}
