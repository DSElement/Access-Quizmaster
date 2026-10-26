namespace QuizMaster.Api.Models;

public class AnswerOption
{
    public Guid Id { get; set; }

    public Guid QuestionId { get; set; }
    public Question? Question { get; set; }

    public int Position { get; set; }

    public required string Text { get; set; }

    public bool IsCorrect { get; set; }
}
