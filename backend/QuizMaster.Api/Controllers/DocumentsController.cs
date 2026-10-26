using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizMaster.Api.Auth;
using QuizMaster.Api.Contracts;
using QuizMaster.Api.Data;
using QuizMaster.Api.Models;
using QuizMaster.Api.Services.Documents;

namespace QuizMaster.Api.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentsController(
    AppDbContext db,
    DocumentStorage storage,
    DocumentQueue queue,
    DocumentSearch search) : ControllerBase
{
    private const int MaxFileNameLength = 255;
    private const int MaxQueryLength = 500;

    [HttpGet]
    public async Task<ActionResult<List<DocumentSummary>>> List(CancellationToken cancellationToken)
    {
        var userId = User.RequireUserId();

        return await db.Documents
            .Where(d => d.OwnerId == userId)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => ToSummary(d))
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentSummary>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.RequireUserId();

        var document = await db.Documents
            .Where(d => d.Id == id && d.OwnerId == userId)
            .Select(d => ToSummary(d))
            .FirstOrDefaultAsync(cancellationToken);

        return document is null ? NotFound() : document;
    }

    [HttpPost]
    [RequestSizeLimit(DocumentFiles.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = DocumentFiles.MaxRequestBytes)]
    public async Task<ActionResult<DocumentSummary>> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        var userId = User.RequireUserId();

        if (file.Length == 0)
            return InvalidFile("The file is empty.");
        if (file.Length > DocumentFiles.MaxFileBytes)
            return InvalidFile($"The file is too big. The limit is {DocumentFiles.MaxFileBytes / (1024 * 1024)} MB.");

        var fileName = Path.GetFileName(file.FileName).Trim();
        if (fileName.Length > MaxFileNameLength)
            fileName = fileName[^MaxFileNameLength..];

        var contentType = DocumentFiles.ContentTypeFor(fileName);
        if (contentType is null)
            return InvalidFile("Only PDF, Markdown (.md) and plain text (.txt) files are supported.");

        await using (var head = file.OpenReadStream())
        {
            if (await DocumentFiles.FindContentProblemAsync(head, contentType, cancellationToken) is { } problem)
                return InvalidFile(problem);
        }

        var id = Guid.CreateVersion7();
        var document = new Document
        {
            Id = id,
            OwnerId = userId,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = file.Length,
            StoredFileName = $"{id}{Path.GetExtension(fileName).ToLowerInvariant()}",
            Status = DocumentStatus.Pending,
            UploadedAt = DateTime.UtcNow,
        };

        await using (var content = file.OpenReadStream())
        {
            await storage.SaveAsync(document.StoredFileName, content, cancellationToken);
        }

        db.Documents.Add(document);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            storage.Delete(document.StoredFileName);
            throw;
        }

        queue.Enqueue(document.Id);

        return CreatedAtAction(nameof(GetById), new { id = document.Id }, ToSummary(document));
    }

    [HttpGet("{id:guid}/chunks")]
    public async Task<ActionResult<List<DocumentChunkView>>> Chunks(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.RequireUserId();

        if (!await db.Documents.AnyAsync(d => d.Id == id && d.OwnerId == userId, cancellationToken))
            return NotFound();

        return await db.DocumentChunks
            .Where(c => c.DocumentId == id)
            .OrderBy(c => c.Index)
            .Select(c => new DocumentChunkView(c.Id, c.Index, c.Text))
            .ToListAsync(cancellationToken);
    }

    // Shows which chunks a question would retrieve; quiz generation uses the same search.
    [HttpGet("{id:guid}/search")]
    public async Task<ActionResult<List<ChunkMatch>>> Search(
        Guid id, [FromQuery] string q, [FromQuery] int top = 5, CancellationToken cancellationToken = default)
    {
        var userId = User.RequireUserId();

        q = q.Trim();
        if (q.Length is 0 or > MaxQueryLength)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid search",
                detail: $"Type a search of 1 to {MaxQueryLength} characters.");
        }

        var status = await db.Documents
            .Where(d => d.Id == id && d.OwnerId == userId)
            .Select(d => (DocumentStatus?)d.Status)
            .FirstOrDefaultAsync(cancellationToken);
        if (status is null) return NotFound();
        if (status != DocumentStatus.Ready)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Document not ready",
                detail: "The document can be searched once it has been indexed.");
        }

        return await search.SearchAsync(id, userId, q, Math.Clamp(top, 1, 10), cancellationToken);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.RequireUserId();

        var document = await db.Documents.FirstOrDefaultAsync(d => d.Id == id && d.OwnerId == userId, cancellationToken);
        if (document is null) return NotFound();

        // The database deletes the chunks too (ON DELETE CASCADE).
        db.Documents.Remove(document);
        await db.SaveChangesAsync(cancellationToken);
        storage.Delete(document.StoredFileName);

        return NoContent();
    }

    private ObjectResult InvalidFile(string detail) =>
        Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid file", detail: detail);

    private static DocumentSummary ToSummary(Document d) => new(
        d.Id, d.FileName, d.ContentType, d.SizeBytes, d.Status, d.Error, d.ChunkCount, d.UploadedAt, d.ProcessedAt);
}
