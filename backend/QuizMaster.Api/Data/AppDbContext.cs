using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuizMaster.Api.Models;

namespace QuizMaster.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configures all the Identity tables.
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(user => user.Property(u => u.DisplayName).HasMaxLength(100));

        modelBuilder.Entity<AppUser>().ToTable("users");
        modelBuilder.Entity<IdentityRole>().ToTable("roles");
        modelBuilder.Entity<IdentityUserRole<string>>().ToTable("user_roles");
        modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("user_claims");
        modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("user_logins");
        modelBuilder.Entity<IdentityUserToken<string>>().ToTable("user_tokens");
        modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("role_claims");

        // Enables pgvector
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Quiz>(quiz =>
        {
            quiz.Property(q => q.Title).HasMaxLength(200);
            quiz.Property(q => q.Description).HasMaxLength(2000);

            quiz.Property(q => q.Visibility).HasConversion<string>().HasMaxLength(20);
            quiz.HasIndex(q => q.Visibility);

            quiz.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(q => q.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
            quiz.HasIndex(q => q.OwnerId);

            quiz.ToTable(t => t.HasCheckConstraint(
                "ck_quizzes_owner_matches_visibility",
                "(visibility = 'Official') = (owner_id IS NULL)"));

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
