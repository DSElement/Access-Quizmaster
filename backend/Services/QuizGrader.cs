using QuizMaster.Contracts;
using QuizMaster.Models;

namespace QuizMaster.Services;

public static class QuizGrader
{
    public static QuizResult Grade(Quiz quiz, IReadOnlyList<SubmitAnswerRequest> answers)
    {
        if (answers.Any(answer => answer is null))
            throw new ArgumentException("Every submitted answer must specify a question.");
        if (answers.Select(a => a.QuestionId).Distinct().Count() != answers.Count)
            throw new ArgumentException("Submit only one answer per question.");

        var questions = quiz.Questions.ToDictionary(q => q.Id);
        foreach (var answer in answers)
        {
            if (!questions.TryGetValue(answer.QuestionId, out var question))
                throw new ArgumentException("An answer refers to a question outside this quiz.");
            if (answer.SelectedOptionId is not null &&
                !question.Options.Any(o => o.Id == answer.SelectedOptionId))
                throw new ArgumentException("An answer option does not belong to its question.");
        }

        var picks = answers.ToDictionary(a => a.QuestionId, a => a.SelectedOptionId);
        var results = quiz.Questions.OrderBy(q => q.Position).Select(q =>
        {
            var selected = picks.GetValueOrDefault(q.Id);
            var correct = q.Options.Single(o => o.IsCorrect);
            return new QuestionResult(q.Id, q.Text, selected, correct.Id, selected == correct.Id,
                q.Options.OrderBy(o => o.Position)
                    .Select(o => new ResultOption(o.Id, o.Text, o.IsCorrect)).ToList());
        }).ToList();
        var correctCount = results.Count(r => r.WasCorrect);
        var percentage = results.Count == 0 ? 0 : Math.Round(100.0 * correctCount / results.Count, 1);
        return new QuizResult(results.Count, correctCount, percentage, results);
    }
}
