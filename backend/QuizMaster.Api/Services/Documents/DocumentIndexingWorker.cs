using Microsoft.EntityFrameworkCore;
using QuizMaster.Api.Data;
using QuizMaster.Api.Models;

namespace QuizMaster.Api.Services.Documents;

// Indexes one document at a time, which also keeps us under the free tier's rate limits.
public class DocumentIndexingWorker(
    DocumentQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<DocumentIndexingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RequeueUnfinishedAsync(stoppingToken);

        await foreach (var documentId in queue.ReadAllAsync(stoppingToken))
        {
            // DbContext is scoped, and a background service has no request scope, so make one per document.
            await using var scope = scopeFactory.CreateAsyncScope();
            var indexer = scope.ServiceProvider.GetRequiredService<DocumentIndexer>();

            try
            {
                await indexer.IndexAsync(documentId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Indexing document {DocumentId} failed unexpectedly", documentId);
            }
        }
    }

    // Documents left Pending or Processing when the API stopped would otherwise wait forever.
    private async Task RequeueUnfinishedAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var unfinished = await db.Documents
                .Where(d => d.Status == DocumentStatus.Pending || d.Status == DocumentStatus.Processing)
                .OrderBy(d => d.UploadedAt)
                .Select(d => d.Id)
                .ToListAsync(cancellationToken);

            foreach (var id in unfinished)
                queue.Enqueue(id);

            if (unfinished.Count > 0)
                logger.LogInformation("Re-queued {Count} unfinished documents", unfinished.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Couldn't re-queue unfinished documents");
        }
    }
}
