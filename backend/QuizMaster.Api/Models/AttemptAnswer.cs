namespace QuizMaster.Api.Models;

public class AttemptAnswer
{
    public Guid Id { get; set; }

    public Guid AttemptId { get; set; }
    public Attempt? Attempt { get; set; }

    public int Position { get; set; }

    public required string QuestionText { get; set; }

    public List<string> Options { get; set; } = [];

    public int? SelectedOptionIndex { get; set; }

    public int CorrectOptionIndex { get; set; }

    public bool WasCorrect { get; set; }
}
