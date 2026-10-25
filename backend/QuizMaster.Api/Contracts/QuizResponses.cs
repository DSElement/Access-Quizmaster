using QuizMaster.Api.Models;

namespace QuizMaster.Api.Contracts;

public record QuizSummary(
    Guid Id,
    string Title,
    string? Description,
    QuizVisibility Visibility,
    int QuestionCount,
    DateTime CreatedAt);

public record QuizDetail(
    Guid Id,
    string Title,
    string? Description,
    QuizVisibility Visibility,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<QuestionDetail> Questions);

public record QuestionDetail(Guid Id, string Text, IReadOnlyList<OptionDetail> Options);

public record OptionDetail(Guid Id, string Text);
