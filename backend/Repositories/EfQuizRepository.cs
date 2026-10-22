using Microsoft.EntityFrameworkCore;
using QuizMaster.Contracts;
using QuizMaster.Data;
using QuizMaster.Models;

namespace QuizMaster.Repositories;

public sealed class EfQuizRepository(QuizzesDbContext database) : IQuizRepository
{
    public Task<List<QuizSummary>> ListAsync(string? callerId, CancellationToken cancellationToken) =>
        database.Quizzes.AsNoTracking().OrderByDescending(q => q.CreatedAt).ThenBy(q => q.Id)
            .Select(q => new QuizSummary(q.Id, q.Title, q.Description, q.CreatedAt,
                q.Questions.Count, q.OwnerId == callerId)).ToListAsync(cancellationToken);

    public Task<List<QuizTitle>> ListTitlesAsync(CancellationToken cancellationToken) =>
        database.Quizzes.AsNoTracking().OrderBy(q => q.CreatedAt)
            .Select(q => new QuizTitle(q.Title, q.Description)).ToListAsync(cancellationToken);

    public Task<Quiz?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        database.Quizzes.Include(q => q.Questions.OrderBy(question => question.Position))
            .ThenInclude(q => q.Options.OrderBy(option => option.Position))
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

    public async Task<Quiz> CreateAsync(string title, string? description, string ownerId,
        IReadOnlyList<CreateQuestionRequest> questions, CancellationToken cancellationToken)
    {
        var quiz = new Quiz
        {
            Title = title.Trim(), Description = description?.Trim(), OwnerId = ownerId,
            Questions = questions.Select((question, position) => NewQuestion(question, position)).ToList()
        };
        database.Quizzes.Add(quiz);
        // EF writes the entire graph in one transaction; failed saves leave no partial quiz.
        await database.SaveChangesAsync(cancellationToken);
        return quiz;
    }

    public async Task UpdateAsync(Quiz quiz, string title, string? description, CancellationToken cancellationToken)
    {
        quiz.Title = title.Trim();
        quiz.Description = description?.Trim();
        await database.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Quiz quiz, CancellationToken cancellationToken)
    {
        database.Quizzes.Remove(quiz);
        await database.SaveChangesAsync(cancellationToken);
    }

    public async Task AddQuestionAsync(Quiz quiz, CreateQuestionRequest input, CancellationToken cancellationToken)
    {
        var position = quiz.Questions.Count == 0 ? 0 : quiz.Questions.Max(q => q.Position) + 1;
        var question = NewQuestion(input, position);
        question.QuizId = quiz.Id;
        database.Questions.Add(question);
        await database.SaveChangesAsync(cancellationToken);
    }

    private static Question NewQuestion(CreateQuestionRequest input, int position) => new()
    {
        Text = input.Text.Trim(), Position = position,
        Options = input.Options.Select((option, index) => new AnswerOption
        {
            Text = option.Text.Trim(), IsCorrect = option.IsCorrect, Position = index
        }).ToList()
    };
}
