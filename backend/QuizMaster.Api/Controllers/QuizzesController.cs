using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizMaster.Api.Auth;
using QuizMaster.Api.Contracts;
using QuizMaster.Api.Data;

namespace QuizMaster.Api.Controllers;

[ApiController]
[Route("api/quizzes")]
public class QuizzesController(AppDbContext db) : ControllerBase
{
    // Browsing is open to visitors; VisibleTo() hides private quizzes that aren't theirs.

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<QuizSummary>>> List(CancellationToken cancellationToken)
    {
        // Projects to DTO, doesn't load questions, just counts them
        var quizzes = await db.Quizzes
            .VisibleTo(User.GetUserId())
            .OrderBy(q => q.Visibility)
            .ThenBy(q => q.Title)
            .Select(q => new QuizSummary(
                q.Id, q.Title, q.Description, q.Visibility, q.Questions.Count, q.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(quizzes);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<QuizDetail>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var quiz = await db.Quizzes
            .VisibleTo(User.GetUserId())
            .Where(q => q.Id == id)
            .Select(q => new QuizDetail(
                q.Id, q.Title, q.Description, q.Visibility, q.CreatedAt, q.UpdatedAt,
                q.Questions
                    .OrderBy(question => question.Position)
                    .Select(question => new QuestionDetail(
                        question.Id,
                        question.Text,
                        question.Options
                            .OrderBy(option => option.Position)
                            .Select(option => new OptionDetail(option.Id, option.Text))
                            .ToList()))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return quiz is null ? NotFound() : Ok(quiz);
    }
}
