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
    public DbSet<Attempt> Attempts => Set<Attempt>();
    public DbSet<AttemptAnswer> AttemptAnswers => Set<AttemptAnswer>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(user =>
        {
            user.Property(u => u.DisplayName).HasMaxLength(100);
            user.Property(u => u.NormalizedDisplayName).HasMaxLength(100);
            user.HasIndex(u => u.NormalizedDisplayName).IsUnique();
        });

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

            quiz.HasOne(q => q.Owner)
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

        modelBuilder.Entity<Attempt>(attempt =>
        {
            attempt.Property(a => a.QuizTitle).HasMaxLength(200);

            attempt.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            attempt.HasIndex(a => new { a.UserId, a.SubmittedAt });

            attempt.HasOne(a => a.Quiz)
                .WithMany()
                .HasForeignKey(a => a.QuizId)
                .OnDelete(DeleteBehavior.SetNull);

            attempt.HasMany(a => a.Answers)
                .WithOne(a => a.Attempt)
                .HasForeignKey(a => a.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AttemptAnswer>(answer =>
        {
            answer.Property(a => a.QuestionText).HasMaxLength(500);
            answer.HasIndex(a => new { a.AttemptId, a.Position });
        });

        modelBuilder.Entity<Document>(document =>
        {
            document.Property(d => d.FileName).HasMaxLength(255);
            document.Property(d => d.ContentType).HasMaxLength(100);
            document.Property(d => d.StoredFileName).HasMaxLength(100);
            document.Property(d => d.Status).HasConversion<string>().HasMaxLength(20);
            document.Property(d => d.Error).HasMaxLength(500);

            document.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
            document.HasIndex(d => new { d.OwnerId, d.UploadedAt });

            document.HasMany(d => d.Chunks)
                .WithOne(c => c.Document)
                .HasForeignKey(c => c.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentChunk>(chunk =>
        {
            chunk.Property(c => c.Embedding).HasColumnType($"vector({DocumentChunk.EmbeddingDimensions})");
            chunk.HasIndex(c => new { c.DocumentId, c.Index }).IsUnique();

            chunk.HasIndex(c => c.Embedding)
                .HasMethod("hnsw")
                .HasOperators("vector_cosine_ops");
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SetTimestamps();
        NormalizeDisplayNames();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        SetTimestamps();
        NormalizeDisplayNames();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void NormalizeDisplayNames()
    {
        foreach (var entry in ChangeTracker.Entries<AppUser>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.NormalizedDisplayName = AppUser.Normalize(entry.Entity.DisplayName);
        }
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
