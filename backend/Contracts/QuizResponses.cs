using QuizMaster.Models;

namespace QuizMaster.Contracts;

public sealed record QuizSummary(
    Guid Id, string Title, string? Description, DateTime CreatedAt, int QuestionCount, bool CanEdit);

public sealed record PlayOption(Guid Id, string Text);
public sealed record PlayQuestion(Guid Id, string Text, IReadOnlyList<PlayOption> Options);
public sealed record QuizDetail(
    Guid Id, string Title, string? Description, DateTime CreatedAt, bool CanEdit,
    IReadOnlyList<PlayQuestion> Questions);

public sealed record ResultOption(Guid Id, string Text, bool IsCorrect);
public sealed record QuestionResult(
    Guid QuestionId, string QuestionText, Guid? SelectedOptionId, Guid CorrectOptionId,
    bool WasCorrect, IReadOnlyList<ResultOption> Options);
public sealed record QuizResult(
    int TotalQuestions, int CorrectCount, double ScorePercentage, IReadOnlyList<QuestionResult> Results);

public static class QuizResponses
{
    public static QuizDetail Detail(Quiz quiz, string? callerId) => new(
        quiz.Id, quiz.Title, quiz.Description, quiz.CreatedAt, quiz.OwnerId == callerId,
        quiz.Questions.OrderBy(q => q.Position)
            .Select(q => new PlayQuestion(q.Id, q.Text,
                q.Options.OrderBy(o => o.Position).Select(o => new PlayOption(o.Id, o.Text)).ToList()))
            .ToList());
}
