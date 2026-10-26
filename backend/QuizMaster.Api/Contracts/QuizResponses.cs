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
    Guid? SourceDocumentId,
    IReadOnlyList<EditableQuestion> Questions);

public record EditableQuestion(Guid Id, string Text, Guid? SourceChunkId, IReadOnlyList<EditableOption> Options);

public record EditableOption(Guid Id, string Text, bool IsCorrect);

public record QuizDraft(
    string Title,
    string? Description,
    Guid SourceDocumentId,
    string SourceDocumentName,
    IReadOnlyList<QuizDraftQuestion> Questions);

public record QuizDraftQuestion(string Text, Guid SourceChunkId, IReadOnlyList<SaveOptionRequest> Options);
