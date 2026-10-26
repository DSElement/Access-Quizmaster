using System.ComponentModel.DataAnnotations;

namespace QuizMaster.Api.Contracts;

public enum QuizScope
{
    Official,
    Mine,
    Community,
}

public record SaveQuizRequest
{
    [Required, StringLength(200, MinimumLength = 3)]
    public string Title { get; init; } = "";

    [StringLength(2000)]
    public string? Description { get; init; }

    [Required, MinLength(1), MaxLength(50)]
    public List<SaveQuestionRequest> Questions { get; init; } = [];
}

public record SaveQuestionRequest
{
    [Required, StringLength(500, MinimumLength = 3)]
    public string Text { get; init; } = "";

    [Required, MinLength(2), MaxLength(6)]
    public List<SaveOptionRequest> Options { get; init; } = [];
}

public record SaveOptionRequest
{
    [Required, StringLength(300)]
    public string Text { get; init; } = "";

    public bool IsCorrect { get; init; }
}
