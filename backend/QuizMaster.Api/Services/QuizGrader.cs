using QuizMaster.Api.Contracts;
using QuizMaster.Api.Models;

namespace QuizMaster.Api.Services;

public static class QuizGrader
{
    public static string? FindProblem(Quiz quiz, IReadOnlyList<SubmittedAnswer> answers)
    {
        if (answers.Any(a => a is null))
            return "An answer is missing.";
        if (answers.Select(a => a.QuestionId).Distinct().Count() != answers.Count)
            return "Submit only one answer per question.";

        var questions = quiz.Questions.ToDictionary(q => q.Id);
        foreach (var answer in answers)
        {
            if (!questions.TryGetValue(answer.QuestionId, out var question))
                return "An answer refers to a question that isn't in this quiz.";
            if (answer.SelectedOptionId is { } optionId && question.Options.All(o => o.Id != optionId))
                return "An answer picks an option that belongs to a different question.";
        }

        return null;
    }

    public static Attempt Grade(Quiz quiz, IReadOnlyList<SubmittedAnswer> answers, string userId)
    {
        var picks = answers.ToDictionary(a => a.QuestionId, a => a.SelectedOptionId);
        var playedOrder = answers.Select((a, i) => (a.QuestionId, i)).ToDictionary(x => x.QuestionId, x => x.i);

        var snapshots = quiz.Questions
            .OrderBy(q => playedOrder.GetValueOrDefault(q.Id, int.MaxValue))
            .ThenBy(q => q.Position)
            .Select((question, index) =>
            {
                var options = question.Options.OrderBy(o => o.Position).ToList();
                var selectedId = picks.GetValueOrDefault(question.Id);
                int? selectedIndex = selectedId is null ? null : options.FindIndex(o => o.Id == selectedId);
                var correctIndex = options.FindIndex(o => o.IsCorrect);

                return new AttemptAnswer
                {
                    Position = index,
                    QuestionText = question.Text,
                    Options = options.Select(o => o.Text).ToList(),
                    SelectedOptionIndex = selectedIndex,
                    CorrectOptionIndex = correctIndex,
                    WasCorrect = selectedIndex == correctIndex,
                };
            })
            .ToList();

        return new Attempt
        {
            UserId = userId,
            QuizId = quiz.Id,
            QuizTitle = quiz.Title,
            SubmittedAt = DateTime.UtcNow,
            TotalQuestions = snapshots.Count,
            CorrectCount = snapshots.Count(a => a.WasCorrect),
            Answers = snapshots,
        };
    }
}
