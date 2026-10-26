using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using QuizMaster.Api.Data;
using QuizMaster.Api.Models;

namespace QuizMaster.Api.Services.Documents;

public record ChunkMatch(Guid ChunkId, int Index, string Text, double Similarity);

public class DocumentSearch(AppDbContext db, IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
{
    // Always filtered by owner, so one user's search can never return another user's text.
    public async Task<List<ChunkMatch>> SearchAsync(
        Guid documentId, string ownerId, string query, int topK, CancellationToken cancellationToken)
    {
        var queryVector = new Vector(await embeddingGenerator.GenerateVectorAsync(query, cancellationToken: cancellationToken));

        // HNSW finds the nearest chunks of *all* documents first and filters afterwards, so a WHERE on one document
        // could return fewer than topK rows. Iterative scan (pgvector 0.8) keeps searching until the filter is satisfied.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlRawAsync("SET LOCAL hnsw.iterative_scan = strict_order", cancellationToken);

        var matches = await db.DocumentChunks
            .Where(c => c.DocumentId == documentId && c.Document!.OwnerId == ownerId)
            .OrderBy(c => c.Embedding.CosineDistance(queryVector))
            .Take(topK)
            .Select(c => new { c.Id, c.Index, c.Text, Distance = c.Embedding.CosineDistance(queryVector) })
            .ToListAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return matches
            .Select(m => new ChunkMatch(m.Id, m.Index, m.Text, Math.Round(1 - m.Distance, 3)))
            .ToList();
    }
}
