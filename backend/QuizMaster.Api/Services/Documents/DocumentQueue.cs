using System.Threading.Channels;

namespace QuizMaster.Api.Services.Documents;

// In-memory queue between the upload endpoint and the indexing worker.
// It's lost on restart, which is fine: the worker re-queues unfinished documents from the database on startup.
public class DocumentQueue
{
    private readonly Channel<Guid> channel = Channel.CreateUnbounded<Guid>();

    public void Enqueue(Guid documentId) => channel.Writer.TryWrite(documentId);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) =>
        channel.Reader.ReadAllAsync(cancellationToken);
}
