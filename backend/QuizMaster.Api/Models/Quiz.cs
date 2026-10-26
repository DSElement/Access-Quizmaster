namespace QuizMaster.Api.Models;

public class Quiz
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public QuizVisibility Visibility { get; set; }

    public string? OwnerId { get; set; }
    public AppUser? Owner { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<Question> Questions { get; set; } = [];
}
