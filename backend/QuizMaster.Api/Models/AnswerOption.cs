namespace QuizMaster.Api.Models;

public class AnswerOption
{
    public Guid Id { get; set; }

    public Guid QuestionId { get; set; }
    public Question? Question { get; set; }

    /// <summary>Order within the question, starting at 0.</summary>
    public int Position { get; set; }

    public required string Text { get; set; }

    public bool IsCorrect { get; set; }
}
