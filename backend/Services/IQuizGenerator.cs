using QuizMaster.Contracts;

namespace QuizMaster.Services;

public sealed record GeneratedQuiz(string Title, List<CreateQuestionRequest> Questions);

public interface IQuizGenerator
{
    Task<GeneratedQuiz> GenerateAsync(string sourceText, int questionCount, CancellationToken cancellationToken);
}

public sealed class QuizGenerationException(string message) : Exception(message);
