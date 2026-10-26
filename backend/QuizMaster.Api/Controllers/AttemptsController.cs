using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizMaster.Api.Auth;
using QuizMaster.Api.Contracts;
using QuizMaster.Api.Data;
using QuizMaster.Api.Models;
using QuizMaster.Api.Services;

namespace QuizMaster.Api.Controllers;

[ApiController]
[Route("api/attempts")]
public class AttemptsController(AppDbContext db) : ControllerBase
{
    [HttpPost("/api/quizzes/{quizId:guid}/attempts")]
    public async Task<ActionResult<AttemptDetail>> Submit(
        Guid quizId, SubmitAttemptRequest request, CancellationToken cancellationToken)
    {
        var userId = User.RequireUserId();

        var quiz = await db.Quizzes
            .AsNoTracking()
            .VisibleTo(userId)
            .Include(q => q.Questions)
            .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == quizId, cancellationToken);
        if (quiz is null) return NotFound();

        if (QuizGrader.FindProblem(quiz, request.Answers) is { } problem)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid answers", detail: problem);

        var attempt = QuizGrader.Grade(quiz, request.Answers, userId);
        db.Attempts.Add(attempt);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = attempt.Id }, ToDetail(attempt));
    }

    [HttpGet]
    public async Task<ActionResult<List<AttemptSummary>>> List(CancellationToken cancellationToken)
    {
        var userId = User.RequireUserId();

        // EF runs everything except Percentage() in SQL; the last Select is allowed to call C# code.
        return await db.Attempts
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.SubmittedAt)
            .Select(a => new AttemptSummary(
                a.Id, a.QuizId, a.QuizTitle, a.SubmittedAt, a.CorrectCount, a.TotalQuestions,
                Percentage(a.CorrectCount, a.TotalQuestions)))
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AttemptDetail>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.RequireUserId();

        var attempt = await db.Attempts
            .AsNoTracking()
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, cancellationToken);

        return attempt is null ? NotFound() : ToDetail(attempt);
    }

    private static double Percentage(int correct, int total) =>
        total == 0 ? 0 : Math.Round(100.0 * correct / total, 1);

    private static AttemptDetail ToDetail(Attempt attempt) => new(
        attempt.Id,
        attempt.QuizId,
        attempt.QuizTitle,
        attempt.SubmittedAt,
        attempt.CorrectCount,
        attempt.TotalQuestions,
        Percentage(attempt.CorrectCount, attempt.TotalQuestions),
        attempt.Answers
            .OrderBy(a => a.Position)
            .Select(a => new AttemptAnswerDetail(
                a.Id, a.QuestionText, a.Options, a.SelectedOptionIndex, a.WasCorrect))
            .ToList());
}
