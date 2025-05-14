using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace CodeVault.Models
{
    public class CodeEmbedding
    {
        public int Id { get; set; }
        public int CodeSnippetId { get; set; }
        public CodeSnippet CodeSnippet { get; set; }

        // The actual embedding array - not directly mapped to DB
        [NotMapped]
        public float[] Embedding { get; set; }

        // A placeholder for EF Core - actual storage is handled by raw SQL
        public string EmbeddingPlaceholder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}