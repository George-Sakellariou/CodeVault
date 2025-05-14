using Microsoft.EntityFrameworkCore;
using CodeVault.Models;
using System;
using OpenAI.Chat;

namespace CodeVault.Data
{
    public class CodeDbContext : DbContext
    {
        public CodeDbContext(DbContextOptions<CodeDbContext> options)
            : base(options)
        {
        }

        public DbSet<CodeSnippet> CodeSnippets { get; set; }
        public DbSet<Models.Conversation> Conversations { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<CodeEmbedding> CodeEmbeddings { get; set; }
        public DbSet<CodeTag> CodeTags { get; set; }
        public DbSet<CodePerformanceMetric> CodePerformanceMetrics { get; set; }
        public DbSet<CodeSecurityScan> CodeSecurityScans { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Add pgvector extension
            modelBuilder.HasPostgresExtension("vector");

            // Configure the Conversation -> ChatMessage relationship
            modelBuilder.Entity<Models.Conversation>()
                .HasMany(c => c.Messages)
                .WithOne(m => m.Conversation)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure the CodeEmbedding entity
            modelBuilder.Entity<CodeEmbedding>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.CodeSnippetId)
                      .IsRequired();

                entity.Property(e => e.EmbeddingPlaceholder)
                      .HasColumnName("Embedding")
                      .HasColumnType("vector(1536)");

                entity.Property(e => e.CreatedAt)
                      .IsRequired();

                entity.HasOne(e => e.CodeSnippet)
                      .WithMany()
                      .HasForeignKey(e => e.CodeSnippetId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure CodeSnippet
            modelBuilder.Entity<CodeSnippet>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired();
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.Language).IsRequired();
                entity.Property(e => e.TagString).HasColumnType("text");
            });

            // Configure CodeTag relationships
            modelBuilder.Entity<CodeTag>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configure CodePerformanceMetric
            modelBuilder.Entity<CodePerformanceMetric>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CodeSnippetId);

                entity.HasOne(e => e.CodeSnippet)
                      .WithMany(c => c.PerformanceMetrics)
                      .HasForeignKey(e => e.CodeSnippetId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure CodeSecurityScan
            modelBuilder.Entity<CodeSecurityScan>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CodeSnippetId);
                entity.HasIndex(e => e.ScanDate);

                entity.HasOne(e => e.CodeSnippet)
                      .WithMany(c => c.SecurityScans)
                      .HasForeignKey(e => e.CodeSnippetId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}