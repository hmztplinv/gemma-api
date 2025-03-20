using LanguageLearningApp.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearningApp.API.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<UserVocabulary> UserVocabularies { get; set; }
        public DbSet<UserProgress> UserProgresses { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<QuizQuestion> QuizQuestions { get; set; }
        public DbSet<QuizResult> QuizResults { get; set; }
        public DbSet<Badge> Badges { get; set; }
        public DbSet<UserBadge> UserBadges { get; set; }
        public DbSet<Goal> Goals { get; set; }
        public DbSet<UserGoal> UserGoals { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.PasswordSalt).IsRequired();
                entity.Property(e => e.LanguageLevel).HasMaxLength(20);

                // Index for faster lookups
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
            });

            // Configure Conversation entity
            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(100);

                // Relationships
                entity.HasOne(c => c.User)
                     .WithMany(u => u.Conversations)
                     .HasForeignKey(c => c.UserId)
                     .OnDelete(DeleteBehavior.Cascade);

                // Index for faster lookups
                entity.HasIndex(e => e.UserId);
            });

            // Configure Message entity
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).IsRequired();

                // ErrorAnalysis alanı için varsayılan değeri boş JSON nesnesi olarak ayarla
                entity.Property(e => e.ErrorAnalysis).HasDefaultValue("{}");

                // Relationships
                entity.HasOne(m => m.Conversation)
                     .WithMany(c => c.Messages)
                     .HasForeignKey(m => m.ConversationId)
                     .OnDelete(DeleteBehavior.Cascade);

                // Index for faster lookups
                entity.HasIndex(e => e.ConversationId);
            });

            // Configure UserVocabulary entity
            modelBuilder.Entity<UserVocabulary>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Word).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Level).HasMaxLength(5);

                // Relationships
                entity.HasOne(v => v.User)
                     .WithMany(u => u.Vocabulary)
                     .HasForeignKey(v => v.UserId)
                     .OnDelete(DeleteBehavior.Cascade);

                // Index for faster lookups
                entity.HasIndex(e => new { e.UserId, e.Word }).IsUnique();
            });

            // Configure UserProgress entity
            modelBuilder.Entity<UserProgress>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Relationships
                entity.HasOne(p => p.User)
                     .WithOne(u => u.Progress)
                     .HasForeignKey<UserProgress>(p => p.UserId)
                     .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Quiz entity
            modelBuilder.Entity<Quiz>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Level).HasMaxLength(5);
                entity.Property(e => e.QuizType).HasMaxLength(50);
            });

            // Configure QuizQuestion entity
            modelBuilder.Entity<QuizQuestion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Question).IsRequired();
                entity.Property(e => e.CorrectAnswer).IsRequired();

                // Relationships
                entity.HasOne(q => q.Quiz)
                     .WithMany(qz => qz.Questions)
                     .HasForeignKey(q => q.QuizId)
                     .OnDelete(DeleteBehavior.Cascade);

                // Index for faster lookups
                entity.HasIndex(e => e.QuizId);
            });

            // Configure QuizResult entity
            modelBuilder.Entity<QuizResult>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Relationships
                entity.HasOne(r => r.User)
                     .WithMany(u => u.QuizResults)
                     .HasForeignKey(r => r.UserId)
                     .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Quiz)
                     .WithMany(q => q.Results)
                     .HasForeignKey(r => r.QuizId)
                     .OnDelete(DeleteBehavior.Cascade);

                // Index for faster lookups
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.QuizId);
            });

            // Configure Badge entity
            modelBuilder.Entity<Badge>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.Property(e => e.RequirementType).HasMaxLength(50);
            });

            // Configure UserBadge entity
            modelBuilder.Entity<UserBadge>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Relationships
                entity.HasOne(ub => ub.User)
                     .WithMany(u => u.Badges)
                     .HasForeignKey(ub => ub.UserId)
                     .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ub => ub.Badge)
                     .WithMany(b => b.UserBadges)
                     .HasForeignKey(ub => ub.BadgeId)
                     .OnDelete(DeleteBehavior.Cascade);

                // Index for faster lookups
                entity.HasIndex(e => new { e.UserId, e.BadgeId }).IsUnique();
            });

            // Configure Goal entity
            modelBuilder.Entity<Goal>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.Property(e => e.GoalType).HasMaxLength(20);
                entity.Property(e => e.TargetType).HasMaxLength(20);
            });

            // Configure UserGoal entity
            modelBuilder.Entity<UserGoal>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Relationships
                entity.HasOne(ug => ug.User)
                     .WithMany(u => u.Goals)
                     .HasForeignKey(ug => ug.UserId)
                     .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ug => ug.Goal)
                     .WithMany(g => g.UserGoals)
                     .HasForeignKey(ug => ug.GoalId)
                     .OnDelete(DeleteBehavior.Cascade);

                // Index for faster lookups
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.GoalId);
            });
        }
    }
}