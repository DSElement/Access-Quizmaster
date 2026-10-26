using System.ClientModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Pgvector;
using QuizMaster.Api.Auth;
using QuizMaster.Api.Data;
using QuizMaster.Api.Models;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Exceptions;

namespace QuizMaster.Api.Services.Documents;

// Extract text -> chunk -> check the limit -> embed -> store. Nothing is sent to Gemini until the chunk count is known.
public class DocumentIndexer(
    AppDbContext db,
    DocumentStorage storage,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    IndexingProgress progress,
    IOptions<DocumentOptions> options,
    ILogger<DocumentIndexer> logger)
{
    // Chunks sent per embeddings request, so a document costs a few requests instead of one per chunk.
    private const int EmbeddingBatchSize = 50;
    private const int MaxRateLimitRetries = 3;

    private readonly DocumentOptions settings = options.Value;

    public async Task IndexAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await db.Documents.FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);
        if (document is null || document.Status is not (DocumentStatus.Pending or DocumentStatus.Processing))
            return;

        document.Status = DocumentStatus.Processing;
        await db.SaveChangesAsync(cancellationToken);
        progress.Report(documentId, new IndexingStep(IndexingStage.Reading, 0, 0));

        try
        {
            var maxChunks = await OwnerIsAdminAsync(document.OwnerId, cancellationToken)
                ? settings.AdminMaxChunks
                : settings.MaxChunks;

            // Twice what maxChunks full chunks hold, because raw text still has spaces and blank lines the chunker drops.
            // Anything longer can't fit; anything shorter is decided by the exact chunk count below.
            var maxChars = maxChunks * settings.ChunkSize * 2;
            var extracted = await TextExtractor.ExtractAsync(
                storage.PathFor(document.StoredFileName), document.ContentType, maxChars, cancellationToken);
            if (extracted.IsTooLong)
                throw new DocumentRejectedException(TooLongMessage(maxChunks));

            var chunks = TextChunker.Split(extracted.Text, settings.ChunkSize, settings.ChunkOverlap);

            if (chunks.Count == 0)
                throw new DocumentRejectedException("No text found. Scanned PDFs (pictures of pages) aren't supported.");

            if (chunks.Count > maxChunks)
                throw new DocumentRejectedException(TooLongMessage(maxChunks, $" It splits into {chunks.Count}."));

            var vectors = await EmbedAsync(documentId, chunks, cancellationToken);

            document.Chunks.AddRange(chunks.Select((chunk, index) => new DocumentChunk
            {
                Index = index,
                Text = chunk,
                Embedding = new Vector(vectors[index]),
            }));
            document.Status = DocumentStatus.Ready;
            document.ChunkCount = chunks.Count;
            document.ProcessedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Indexed document {DocumentId} into {ChunkCount} chunks", documentId, chunks.Count);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The API is shutting down; the document stays Processing and is re-queued on the next start.
            throw;
        }
        catch (DocumentRejectedException ex)
        {
            await MarkFailedAsync(documentId, ex.Message, cancellationToken);
        }
        catch (Exception ex)
        {
            db.ChangeTracker.Clear();
            if (!await db.Documents.AnyAsync(d => d.Id == documentId, cancellationToken))
            {
                logger.LogInformation("Document {DocumentId} was deleted while it was being indexed", documentId);
                return;
            }

            logger.LogError(ex, "Indexing document {DocumentId} failed", documentId);
            await MarkFailedAsync(documentId, Describe(ex), cancellationToken);
        }
        finally
        {
            progress.Clear(documentId);
        }
    }

    // Read from the database: the worker has no signed-in user, and this way a promotion counts immediately.
    private Task<bool> OwnerIsAdminAsync(string ownerId, CancellationToken cancellationToken) =>
        db.UserRoles.AnyAsync(
            userRole => userRole.UserId == ownerId &&
                db.Roles.Any(role => role.Id == userRole.RoleId && role.Name == Roles.Admin),
            cancellationToken);

    private static string TooLongMessage(int maxChunks, string detail = "") =>
        $"This document is too long: the limit is {maxChunks} chunks of text.{detail} " +
        "Try a shorter document, or split it into parts.";

    private async Task<List<ReadOnlyMemory<float>>> EmbedAsync(
        Guid documentId, List<string> chunks, CancellationToken cancellationToken)
    {
        var vectors = new List<ReadOnlyMemory<float>>(chunks.Count);

        foreach (var batch in chunks.Chunk(EmbeddingBatchSize))
        {
            progress.Report(documentId, new IndexingStep(IndexingStage.Embedding, vectors.Count, chunks.Count));

            var embeddings = await WithRateLimitRetriesAsync(
                () => embeddingGenerator.GenerateAsync(batch, cancellationToken: cancellationToken),
                onWait: () => progress.Report(documentId,
                    new IndexingStep(IndexingStage.RateLimited, vectors.Count, chunks.Count)),
                cancellationToken);

            foreach (var embedding in embeddings)
            {
                if (embedding.Vector.Length != DocumentChunk.EmbeddingDimensions)
                {
                    throw new InvalidOperationException(
                        $"Expected {DocumentChunk.EmbeddingDimensions} embedding dimensions, got {embedding.Vector.Length}.");
                }
                vectors.Add(embedding.Vector);
            }
        }

        return vectors;
    }

    // The free tier limits requests and tokens per minute; the SDK's own retries are too quick to wait that out.
    private async Task<T> WithRateLimitRetriesAsync<T>(
        Func<Task<T>> call, Action onWait, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await call();
            }
            catch (ClientResultException ex) when (ex.Status == 429 && attempt <= MaxRateLimitRetries)
            {
                var delay = TimeSpan.FromSeconds(20 * attempt);
                logger.LogWarning("Gemini rate limit reached, retrying in {Delay}", delay);
                onWait();
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private async Task MarkFailedAsync(Guid documentId, string error, CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        await db.Documents
            .Where(d => d.Id == documentId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(d => d.Status, DocumentStatus.Failed)
                .SetProperty(d => d.Error, error)
                .SetProperty(d => d.ProcessedAt, DateTime.UtcNow), cancellationToken);
    }

    private static string Describe(Exception ex) => ex switch
    {
        PdfDocumentEncryptedException => "This PDF is password-protected.",
        PdfDocumentFormatException => "This PDF is damaged and couldn't be read.",
        ClientResultException { Status: 429 } =>
            "The AI service's rate limit was reached. Delete the document and upload it again in a few minutes.",
        ClientResultException { Status: 401 or 403 } => "The AI service rejected the API key.",
        ClientResultException => "The AI service couldn't create the embeddings.",
        _ => "Something went wrong while processing this document.",
    };

    // A problem with the document itself, shown to the user as is.
    private sealed class DocumentRejectedException(string message) : Exception(message);
}
