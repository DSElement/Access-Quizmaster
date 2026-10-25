namespace QuizMaster.Api.Models;

public class Quiz
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public QuizVisibility Visibility { get; set; }

    /// <summary>The user who created the quiz. Null for official quizzes.</summary>
    public string? OwnerId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<Question> Questions { get; set; } = [];
}
