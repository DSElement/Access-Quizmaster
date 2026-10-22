using System.ComponentModel.DataAnnotations;

namespace QuizMaster.Contracts;

public sealed class CreateQuizRequest
{
    [Required, StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = "";

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    public List<CreateQuestionRequest> Questions { get; set; } = [];
}

public sealed class UpdateQuizRequest
{
    [Required, StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = "";

    [StringLength(2000)]
    public string? Description { get; set; }
}

public sealed class CreateQuestionRequest
{
    [Required, StringLength(500, MinimumLength = 3)]
    public string Text { get; set; } = "";

    [Required, MinLength(2)]
    public List<CreateOptionRequest> Options { get; set; } = [];
}

public sealed class CreateOptionRequest
{
    [Required, StringLength(300, MinimumLength = 1)]
    public string Text { get; set; } = "";

    public bool IsCorrect { get; set; }
}

public sealed class GenerateQuizRequest
{
    [Required, StringLength(20000, MinimumLength = 200)]
    public string SourceText { get; set; } = "";

    [StringLength(200, MinimumLength = 3)]
    public string? Title { get; set; }

    [Range(3, 10)]
    public int QuestionCount { get; set; } = 5;
}

public sealed class SubmitQuizRequest
{
    [Required]
    public List<SubmitAnswerRequest> Answers { get; set; } = [];
}

public sealed record SubmitAnswerRequest(Guid QuestionId, Guid? SelectedOptionId);
