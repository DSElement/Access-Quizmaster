using Microsoft.EntityFrameworkCore;
using QuizMaster.Api.Models;

namespace QuizMaster.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Enables pgvector
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Quiz>(quiz =>
        {
            quiz.Property(q => q.Title).HasMaxLength(200);
            quiz.Property(q => q.Description).HasMaxLength(2000);

            quiz.Property(q => q.Visibility).HasConversion<string>().HasMaxLength(20);
            quiz.HasIndex(q => q.Visibility);

            quiz.Property(q => q.OwnerId).HasMaxLength(450);
            quiz.HasIndex(q => q.OwnerId);

            // Official quizzes have no owner
            quiz.ToTable(t => t.HasCheckConstraint(
                "ck_quizzes_owner_matches_visibility",
                "(visibility = 'Official') = (owner_id IS NULL)"));

            // Deleting a quiz deletes its questions (and, below, their options).
            quiz.HasMany(q => q.Questions)
                .WithOne(q => q.Quiz)
                .HasForeignKey(q => q.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Question>(question =>
        {
            question.Property(q => q.Text).HasMaxLength(500);
            question.HasIndex(q => new { q.QuizId, q.Position });

            question.HasMany(q => q.Options)
                .WithOne(o => o.Question)
                .HasForeignKey(o => o.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AnswerOption>(option =>
        {
            option.Property(o => o.Text).HasMaxLength(300);
            option.HasIndex(o => new { o.QuestionId, o.Position });
        });
    }

    // Every save goes through these two methods, so timestamps are set in one place
    // instead of in every controller.
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SetTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        SetTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void SetTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Quiz>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
