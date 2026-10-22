using QuizMaster.Contracts;
using QuizMaster.Models;

namespace QuizMaster.Repositories;

public sealed record QuizTitle(string Title, string? Description);

public interface IQuizRepository
{
    Task<List<QuizSummary>> ListAsync(string? callerId, CancellationToken cancellationToken);
    Task<List<QuizTitle>> ListTitlesAsync(CancellationToken cancellationToken);
    Task<Quiz?> FindAsync(Guid id, CancellationToken cancellationToken);
    Task<Quiz> CreateAsync(string title, string? description, string ownerId,
        IReadOnlyList<CreateQuestionRequest> questions, CancellationToken cancellationToken);
    Task UpdateAsync(Quiz quiz, string title, string? description, CancellationToken cancellationToken);
    Task DeleteAsync(Quiz quiz, CancellationToken cancellationToken);
    Task AddQuestionAsync(Quiz quiz, CreateQuestionRequest input, CancellationToken cancellationToken);
}
