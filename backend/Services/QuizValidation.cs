using QuizMaster.Contracts;

namespace QuizMaster.Services;

public static class QuizValidation
{
    public static string? Question(CreateQuestionRequest? question)
    {
        if (question is null || string.IsNullOrWhiteSpace(question.Text) || question.Text.Trim().Length is < 3 or > 500)
            return "Question text must be between 3 and 500 characters.";
        if (question.Options is null || question.Options.Count < 2)
            return "Each question needs at least two answer options.";
        if (question.Options.Any(o => o is null || string.IsNullOrWhiteSpace(o.Text) || o.Text.Trim().Length > 300))
            return "Every answer option needs between 1 and 300 characters.";
        if (question.Options.Count(o => o.IsCorrect) != 1)
            return "Exactly one answer option must be marked correct per question.";
        return null;
    }

    public static string? Generated(GeneratedQuiz quiz, int expectedCount)
    {
        if (string.IsNullOrWhiteSpace(quiz.Title) || quiz.Title.Trim().Length is < 3 or > 200)
            return "The quiz title must be between 3 and 200 characters.";
        if (quiz.Questions is null || quiz.Questions.Count != expectedCount)
            return $"Provide exactly {expectedCount} questions.";
        foreach (var question in quiz.Questions)
        {
            var error = Question(question);
            if (error is not null) return error;
            if (question.Options.Count != 4)
                return "Generated questions must have exactly four answer options.";
        }
        return null;
    }
}
