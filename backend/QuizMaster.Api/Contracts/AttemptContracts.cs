using System.ComponentModel.DataAnnotations;

namespace QuizMaster.Api.Contracts;

public record SubmitAttemptRequest
{
    [Required]
    public List<SubmittedAnswer> Answers { get; init; } = [];
}

public record SubmittedAnswer(Guid QuestionId, Guid? SelectedOptionId);

public record AttemptSummary(
    Guid Id,
    Guid? QuizId,
    string QuizTitle,
    DateTime SubmittedAt,
    int CorrectCount,
    int TotalQuestions,
    double ScorePercentage);

public record AttemptDetail(
    Guid Id,
    Guid? QuizId,
    string QuizTitle,
    DateTime SubmittedAt,
    int CorrectCount,
    int TotalQuestions,
    double ScorePercentage,
    IReadOnlyList<AttemptAnswerDetail> Answers);

public record AttemptAnswerDetail(
    Guid Id,
    string QuestionText,
    IReadOnlyList<string> Options,
    int? SelectedOptionIndex,
    bool WasCorrect);
