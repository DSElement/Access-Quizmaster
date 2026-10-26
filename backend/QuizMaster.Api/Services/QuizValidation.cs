using QuizMaster.Api.Contracts;

namespace QuizMaster.Api.Services;

public record ValidationError(string Field, string Message);

public static class QuizValidation
{
    public static List<ValidationError> Validate(SaveQuizRequest request)
    {
        var errors = new List<ValidationError>();

        if (request.Title.Trim().Length < 3)
            errors.Add(new("title", "The title needs at least 3 characters."));

        for (var i = 0; i < request.Questions.Count; i++)
        {
            var question = request.Questions[i];
            var field = $"questions[{i}]";

            if (question is null)
            {
                errors.Add(new(field, "Question is missing."));
                continue;
            }

            if (question.Text.Trim().Length < 3)
                errors.Add(new($"{field}.text", "The question needs at least 3 characters."));

            if (question.Options.Any(o => o is null))
            {
                errors.Add(new($"{field}.options", "An answer option is missing."));
                continue;
            }

            if (question.Options.Count(o => o.IsCorrect) != 1)
                errors.Add(new($"{field}.options", "Mark exactly one option as correct."));

            var distinctTexts = question.Options
                .Select(o => o.Text.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            if (distinctTexts != question.Options.Count)
                errors.Add(new($"{field}.options", "Answer options must be different from each other."));
        }

        return errors;
    }
}
