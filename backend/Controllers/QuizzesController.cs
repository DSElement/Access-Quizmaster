using System.ClientModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizMaster.Contracts;
using QuizMaster.Repositories;
using QuizMaster.Services;

namespace QuizMaster.Controllers;

[ApiController]
[Route("quizzes")]
[Authorize(Policy = "QuizWriter")]
public sealed class QuizzesController(
    IQuizRepository repository, IQuizGenerator generator, ILogger<QuizzesController> logger) : ControllerBase
{
    private string? CallerId => User.FindFirst("oid")?.Value;

    [HttpGet, AllowAnonymous]
    public async Task<ActionResult<List<QuizSummary>>> List(CancellationToken cancellationToken) =>
        Ok(await repository.ListAsync(CallerId, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QuizDetail>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var quiz = await repository.FindAsync(id, cancellationToken);
        return quiz is null ? NotFound() : Ok(QuizResponses.Detail(quiz, CallerId));
    }

    [HttpPost]
    public async Task<ActionResult<QuizDetail>> Create(CreateQuizRequest request, CancellationToken cancellationToken)
    {
        if (request.Title.Trim().Length < 3) return Invalid("A quiz title needs at least 3 characters.");
        foreach (var question in request.Questions)
            if (QuizValidation.Question(question) is { } error) return Invalid(error);

        var quiz = await repository.CreateAsync(request.Title, request.Description, CallerId!, request.Questions, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = quiz.Id }, QuizResponses.Detail(quiz, CallerId));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateQuizRequest request, CancellationToken cancellationToken)
    {
        if (request.Title.Trim().Length < 3) return Invalid("A quiz title needs at least 3 characters.");
        var quiz = await repository.FindAsync(id, cancellationToken);
        if (quiz is null) return NotFound();
        if (quiz.OwnerId != CallerId) return Forbid();
        await repository.UpdateAsync(quiz, request.Title, request.Description, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var quiz = await repository.FindAsync(id, cancellationToken);
        if (quiz is null) return NotFound();
        if (quiz.OwnerId != CallerId) return Forbid();
        await repository.DeleteAsync(quiz, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/questions")]
    public async Task<ActionResult<QuizDetail>> AddQuestion(Guid id, CreateQuestionRequest request, CancellationToken cancellationToken)
    {
        var quiz = await repository.FindAsync(id, cancellationToken);
        if (quiz is null) return NotFound();
        if (quiz.OwnerId != CallerId) return Forbid();
        if (QuizValidation.Question(request) is { } error) return Invalid(error);
        await repository.AddQuestionAsync(quiz, request, cancellationToken);
        return Ok(QuizResponses.Detail(quiz, CallerId));
    }

    [HttpPost("generate")]
    public async Task<ActionResult<QuizDetail>> Generate(GenerateQuizRequest request, CancellationToken cancellationToken)
    {
        if (request.SourceText.Trim().Length < 200) return Invalid("Paste at least 200 characters of source text.");
        if (request.Title is not null && request.Title.Trim().Length < 3)
            return Invalid("A supplied quiz title needs at least 3 characters.");

        GeneratedQuiz generated;
        try
        {
            generated = await generator.GenerateAsync(request.SourceText.Trim(), request.QuestionCount, cancellationToken);
        }
        catch (QuizGenerationException exception)
        {
            logger.LogWarning(exception, "Quiz generation did not produce a valid result");
            return Problem(statusCode: 502, title: "Quiz generation failed", detail: exception.Message);
        }
        catch (Exception exception) when (exception is ClientResultException or HttpRequestException)
        {
            logger.LogWarning(exception, "Ollama request failed");
            return Problem(statusCode: 502, title: "Ollama request failed",
                detail: "Check that Ollama is running and the configured model has been downloaded.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Problem(statusCode: 504, title: "Quiz generation timed out",
                detail: "The local model did not finish within the configured time limit.");
        }

        if (QuizValidation.Generated(generated, request.QuestionCount) is { } error)
            return Problem(statusCode: 502, title: "Quiz generation failed", detail: error);

        var title = request.Title is null ? generated.Title : request.Title;
        var quiz = await repository.CreateAsync(title, "Generated from source text using local AI.",
            CallerId!, generated.Questions, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = quiz.Id }, QuizResponses.Detail(quiz, CallerId));
    }

    [HttpGet("{id:guid}/play"), AllowAnonymous]
    public async Task<ActionResult<QuizDetail>> Play(Guid id, CancellationToken cancellationToken)
    {
        var quiz = await repository.FindAsync(id, cancellationToken);
        return quiz is null ? NotFound() : Ok(QuizResponses.Detail(quiz, CallerId));
    }

    [HttpPost("{id:guid}/evaluate"), AllowAnonymous]
    public async Task<ActionResult<QuizResult>> Evaluate(Guid id, SubmitQuizRequest request, CancellationToken cancellationToken)
    {
        var quiz = await repository.FindAsync(id, cancellationToken);
        if (quiz is null) return NotFound();
        try
        {
            return Ok(QuizGrader.Grade(quiz, request.Answers));
        }
        catch (ArgumentException exception)
        {
            return Invalid(exception.Message);
        }
    }

    private ObjectResult Invalid(string detail) => Problem(statusCode: 400, title: "Invalid quiz data", detail: detail);
}
