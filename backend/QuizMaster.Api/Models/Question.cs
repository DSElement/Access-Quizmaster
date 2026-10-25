namespace QuizMaster.Api.Models;

public class Question
{
    public Guid Id { get; set; }

    public Guid QuizId { get; set; }
    public Quiz? Quiz { get; set; }

    public int Position { get; set; }

    public required string Text { get; set; }

    public List<AnswerOption> Options { get; set; } = [];
}
