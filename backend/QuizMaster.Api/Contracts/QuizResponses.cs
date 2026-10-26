using QuizMaster.Api.Models;

namespace QuizMaster.Api.Contracts;

public record QuizSummary(
    Guid Id,
    string Title,
    string? Description,
    QuizVisibility Visibility,
    string? AuthorName,
    int QuestionCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool CanEdit,
    bool CanDelete,
    bool CanPublish);

public record PlayQuiz(Guid Id, string Title, IReadOnlyList<PlayQuestion> Questions);

public record PlayQuestion(Guid Id, string Text, IReadOnlyList<PlayOption> Options);

public record PlayOption(Guid Id, string Text);

public record EditableQuiz(
    Guid Id,
    string Title,
    string? Description,
    QuizVisibility Visibility,
    IReadOnlyList<EditableQuestion> Questions);

public record EditableQuestion(Guid Id, string Text, IReadOnlyList<EditableOption> Options);

public record EditableOption(Guid Id, string Text, bool IsCorrect);
