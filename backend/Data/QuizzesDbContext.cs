using Microsoft.EntityFrameworkCore;
using QuizMaster.Models;

namespace QuizMaster.Data;

public sealed class QuizzesDbContext(DbContextOptions<QuizzesDbContext> options) : DbContext(options)
{
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Quiz>(entity =>
        {
            entity.Property(q => q.Id).ValueGeneratedNever();
            entity.Property(q => q.Title).HasMaxLength(200).IsRequired();
            entity.Property(q => q.Description).HasMaxLength(2000);
            entity.Property(q => q.OwnerId).HasMaxLength(100).IsRequired();
            entity.HasMany(q => q.Questions).WithOne().HasForeignKey(q => q.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<Question>(entity =>
        {
            entity.Property(q => q.Id).ValueGeneratedNever();
            entity.Property(q => q.Text).HasMaxLength(500).IsRequired();
            entity.HasIndex(q => new { q.QuizId, q.Position });
            entity.HasMany(q => q.Options).WithOne().HasForeignKey(o => o.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<AnswerOption>(entity =>
        {
            entity.Property(o => o.Id).ValueGeneratedNever();
            entity.Property(o => o.Text).HasMaxLength(300).IsRequired();
            entity.HasIndex(o => new { o.QuestionId, o.Position });
        });
        DemoQuizzes.Seed(model);
    }
}
