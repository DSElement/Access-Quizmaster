namespace QuizMaster.Api.Models;

public class Attempt
{
    public Guid Id { get; set; }

    public required string UserId { get; set; }

    public Guid? QuizId { get; set; }
    public Quiz? Quiz { get; set; }

    public required string QuizTitle { get; set; }

    public DateTime SubmittedAt { get; set; }

    public int CorrectCount { get; set; }
    public int TotalQuestions { get; set; }

    public List<AttemptAnswer> Answers { get; set; } = [];
}
