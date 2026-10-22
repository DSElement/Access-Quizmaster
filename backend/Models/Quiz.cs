namespace QuizMaster.Models;

public sealed class Quiz
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string OwnerId { get; set; } = "";
    public List<Question> Questions { get; set; } = [];
}

public sealed class Question
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuizId { get; set; }
    public string Text { get; set; } = "";
    public int Position { get; set; }
    public List<AnswerOption> Options { get; set; } = [];
}

public sealed class AnswerOption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionId { get; set; }
    public string Text { get; set; } = "";
    public int Position { get; set; }
    public bool IsCorrect { get; set; }
}
