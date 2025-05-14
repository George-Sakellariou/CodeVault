using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeVault.Models
{
    public class CodeSnippet
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Content { get; set; }
        public required string Language { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // We'll store tags as a comma-separated string in the database
        public string TagString { get; set; } = string.Empty;

        [NotMapped]
        [JsonIgnore]
        public string[] Tags
        {
            get => string.IsNullOrEmpty(TagString)
                ? Array.Empty<string>()
                : TagString.Split(',', StringSplitOptions.RemoveEmptyEntries);
            set => TagString = string.Join(',', value);
        }

        // Related metrics
        public List<CodePerformanceMetric> PerformanceMetrics { get; set; } = new List<CodePerformanceMetric>();

        // Security scans
        public List<CodeSecurityScan> SecurityScans { get; set; } = new List<CodeSecurityScan>();

        // Additional metadata
        public int ViewCount { get; set; } = 0;
        public int UsageCount { get; set; } = 0;
        public double Rating { get; set; } = 0;
        public int RatingCount { get; set; } = 0;
    }
}