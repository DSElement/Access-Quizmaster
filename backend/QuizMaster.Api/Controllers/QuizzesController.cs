using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizMaster.Api.Auth;
using QuizMaster.Api.Contracts;
using QuizMaster.Api.Data;
using QuizMaster.Api.Models;
using QuizMaster.Api.Services;

namespace QuizMaster.Api.Controllers;

[ApiController]
[Route("api/quizzes")]
public class QuizzesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<QuizSummary>>> List(
        [FromQuery] QuizScope? scope, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (scope == QuizScope.Mine && userId is null) return Unauthorized();

        IQueryable<Quiz> query = db.Quizzes.VisibleTo(userId);
        query = scope switch
        {
            QuizScope.Official => query
                .Where(q => q.Visibility == QuizVisibility.Official)
                .OrderBy(q => q.Title),
            QuizScope.Mine => query
                .Where(q => q.OwnerId == userId)
                .OrderByDescending(q => q.UpdatedAt),
            QuizScope.Community => query
                .Where(q => q.Visibility == QuizVisibility.Public)
                .OrderByDescending(q => q.UpdatedAt),
            _ => query
                .OrderBy(q => q.Visibility)
                .ThenBy(q => q.Title),
        };

        var rows = await SelectRows(query).ToListAsync(cancellationToken);
        return rows.Select(ToSummary).ToList();
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<QuizSummary>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var row = await SelectRows(db.Quizzes.VisibleTo(User.GetUserId()).Where(q => q.Id == id))
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? NotFound() : ToSummary(row);
    }

    [HttpGet("{id:guid}/play")]
    public async Task<ActionResult<PlayQuiz>> Play(Guid id, CancellationToken cancellationToken)
    {
        var quiz = await db.Quizzes
            .VisibleTo(User.GetUserId())
            .Where(q => q.Id == id)
            .Select(q => new PlayQuiz(
                q.Id,
                q.Title,
                q.Questions
                    .OrderBy(question => question.Position)
                    .Select(question => new PlayQuestion(
                        question.Id,
                        question.Text,
                        question.Options
                            .OrderBy(option => option.Position)
                            .Select(option => new PlayOption(option.Id, option.Text))
                            .ToList()))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return quiz is null ? NotFound() : Ok(quiz);
    }

    [HttpGet("{id:guid}/edit")]
    public async Task<ActionResult<EditableQuiz>> GetForEdit(Guid id, CancellationToken cancellationToken)
    {
        var quiz = await LoadWithQuestions(id, cancellationToken);
        if (quiz is null) return NotFound();
        if (!User.CanEditQuiz(quiz.OwnerId, quiz.Visibility)) return Forbid();

        return ToEditable(quiz);
    }

    [HttpPost("{id:guid}/make-official")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> MakeQuizOfficial(
        Guid id, CancellationToken cancellationToken)
    {
        var quiz = await db.Quizzes
            .VisibleTo(User.GetUserId())
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quiz is null) return NotFound();
        if (!User.CanMakeOfficial(quiz.OwnerId)) return Forbid();

        quiz.Visibility = QuizVisibility.Official;
        quiz.OwnerId = null;
        quiz.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost]
    public Task<ActionResult<EditableQuiz>> Create(SaveQuizRequest request, CancellationToken cancellationToken) =>
        CreateQuiz(request, QuizVisibility.Private, User.RequireUserId(), cancellationToken);

    [HttpPost("official")]
    [Authorize(Policy = Policies.AdminOnly)]
    public Task<ActionResult<EditableQuiz>> CreateOfficial(SaveQuizRequest request, CancellationToken cancellationToken) =>
        CreateQuiz(request, QuizVisibility.Official, ownerId: null, cancellationToken);

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EditableQuiz>> Update(
        Guid id, SaveQuizRequest request, CancellationToken cancellationToken)
    {
        var quiz = await LoadWithQuestions(id, cancellationToken);
        if (quiz is null) return NotFound();
        if (!User.CanEditQuiz(quiz.OwnerId, quiz.Visibility)) return Forbid();
        if (ValidationErrors(request) is { } invalid) return invalid;
        if (await SourceErrorsAsync(quiz.SourceDocumentId, request, mustOwnDocument: false, cancellationToken) is { } badSource)
            return badSource;

        quiz.Title = request.Title.Trim();
        quiz.Description = CleanDescription(request.Description);

        db.Questions.RemoveRange(quiz.Questions);
        quiz.Questions = ToQuestions(request);

        quiz.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return ToEditable(quiz);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var quiz = await db.Quizzes
            .VisibleTo(User.GetUserId())
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quiz is null) return NotFound();
        if (!User.CanDeleteQuiz(quiz.OwnerId, quiz.Visibility)) return Forbid();

        db.Quizzes.Remove(quiz);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/publish")]
    public Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken) =>
        SetVisibility(id, QuizVisibility.Public, cancellationToken);

    [HttpPost("{id:guid}/unpublish")]
    public Task<IActionResult> Unpublish(Guid id, CancellationToken cancellationToken) =>
        SetVisibility(id, QuizVisibility.Private, cancellationToken);

    private async Task<IActionResult> SetVisibility(
        Guid id, QuizVisibility visibility, CancellationToken cancellationToken)
    {
        var quiz = await db.Quizzes
            .VisibleTo(User.GetUserId())
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quiz is null) return NotFound();
        if (!User.CanPublishQuiz(quiz.OwnerId)) return Forbid();

        quiz.Visibility = visibility;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<ActionResult<EditableQuiz>> CreateQuiz(
        SaveQuizRequest request, QuizVisibility visibility, string? ownerId, CancellationToken cancellationToken)
    {
        if (ValidationErrors(request) is { } invalid) return invalid;
        if (await SourceErrorsAsync(request.SourceDocumentId, request, mustOwnDocument: true, cancellationToken) is { } badSource)
            return badSource;

        var quiz = new Quiz
        {
            Title = request.Title.Trim(),
            Description = CleanDescription(request.Description),
            Visibility = visibility,
            OwnerId = ownerId,
            SourceDocumentId = request.SourceDocumentId,
            Questions = ToQuestions(request),
        };

        db.Quizzes.Add(quiz);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = quiz.Id }, ToEditable(quiz));
    }

    private static IQueryable<QuizRow> SelectRows(IQueryable<Quiz> quizzes) =>
        quizzes.Select(q => new QuizRow(
            q.Id, q.Title, q.Description, q.Visibility, q.OwnerId,
            q.Owner == null ? null : q.Owner.DisplayName,
            q.Questions.Count, q.CreatedAt, q.UpdatedAt));

    private QuizSummary ToSummary(QuizRow row) => new(
        row.Id, row.Title, row.Description, row.Visibility, row.AuthorName, row.QuestionCount,
        row.CreatedAt, row.UpdatedAt,
        CanEdit: User.CanEditQuiz(row.OwnerId, row.Visibility),
        CanDelete: User.CanDeleteQuiz(row.OwnerId, row.Visibility),
        CanPublish: User.CanPublishQuiz(row.OwnerId),
        CanMakeOfficial: User.CanMakeOfficial(row.OwnerId));

    private record QuizRow(
        Guid Id, string Title, string? Description, QuizVisibility Visibility, string? OwnerId,
        string? AuthorName, int QuestionCount, DateTime CreatedAt, DateTime UpdatedAt);

    private Task<Quiz?> LoadWithQuestions(Guid id, CancellationToken cancellationToken) =>
        db.Quizzes
            .VisibleTo(User.GetUserId())
            .Include(q => q.Questions)
            .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

    private ActionResult? ValidationErrors(SaveQuizRequest request)
    {
        var errors = QuizValidation.Validate(request);
        if (errors.Count == 0) return null;

        foreach (var error in errors)
            ModelState.AddModelError(error.Field, error.Message);
        return ValidationProblem(ModelState);
    }

    private async Task<ActionResult?> SourceErrorsAsync(
        Guid? documentId, SaveQuizRequest request, bool mustOwnDocument, CancellationToken cancellationToken)
    {
        var chunkIds = request.Questions.Select(q => q.SourceChunkId).OfType<Guid>().Distinct().ToList();

        if (documentId is null)
        {
            if (chunkIds.Count == 0) return null;
            ModelState.AddModelError("questions", "Questions can only cite passages of the quiz's source document.");
            return ValidationProblem(ModelState);
        }

        if (mustOwnDocument)
        {
            var userId = User.RequireUserId();
            var ownsDocument = await db.Documents.AnyAsync(
                d => d.Id == documentId && d.OwnerId == userId && d.Status == DocumentStatus.Ready, cancellationToken);
            if (!ownsDocument)
            {
                ModelState.AddModelError("sourceDocumentId", "The source document doesn't exist or isn't yours.");
                return ValidationProblem(ModelState);
            }
        }

        var found = await db.DocumentChunks.CountAsync(
            c => c.DocumentId == documentId && chunkIds.Contains(c.Id), cancellationToken);
        if (found == chunkIds.Count) return null;

        ModelState.AddModelError("questions", "A question cites a passage that isn't part of the source document.");
        return ValidationProblem(ModelState);
    }

    private static string? CleanDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static List<Question> ToQuestions(SaveQuizRequest request) =>
        request.Questions
            .Select((question, i) => new Question
            {
                Position = i,
                Text = question.Text.Trim(),
                SourceChunkId = question.SourceChunkId,
                Options = question.Options
                    .Select((option, j) => new AnswerOption
                    {
                        Position = j,
                        Text = option.Text.Trim(),
                        IsCorrect = option.IsCorrect,
                    })
                    .ToList(),
            })
            .ToList();

    private static EditableQuiz ToEditable(Quiz quiz) => new(
        quiz.Id,
        quiz.Title,
        quiz.Description,
        quiz.Visibility,
        quiz.SourceDocumentId,
        quiz.Questions
            .OrderBy(q => q.Position)
            .Select(q => new EditableQuestion(
                q.Id,
                q.Text,
                q.SourceChunkId,
                q.Options
                    .OrderBy(o => o.Position)
                    .Select(o => new EditableOption(o.Id, o.Text, o.IsCorrect))
                    .ToList()))
            .ToList());
}
