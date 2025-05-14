using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeVault.Models
{
    public class CodeSecurityScan
    {
        public int Id { get; set; }
        public int CodeSnippetId { get; set; }
        public CodeSnippet CodeSnippet { get; set; }

        public DateTime ScanDate { get; set; } = DateTime.UtcNow;
        public string Scanner { get; set; } = string.Empty; // e.g., "Semgrep", "ESLint", "AI Analysis"

        // Overall score, if applicable (0-100)
        public int? SecurityScore { get; set; }

        // Count of issues by severity
        public int CriticalIssues { get; set; } = 0;
        public int HighIssues { get; set; } = 0;
        public int MediumIssues { get; set; } = 0;
        public int LowIssues { get; set; } = 0;

        // We'll store detailed findings as JSON
        public string FindingsJson { get; set; } = string.Empty;

        [NotMapped]
        [JsonIgnore]
        public List<SecurityFinding> Findings
        {
            get => string.IsNullOrEmpty(FindingsJson)
                ? new List<SecurityFinding>()
                : JsonSerializer.Deserialize<List<SecurityFinding>>(FindingsJson);
            set => FindingsJson = JsonSerializer.Serialize(value);
        }
    }

    public class SecurityFinding
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; } // "Critical", "High", "Medium", "Low"
        public int LineNumber { get; set; }
        public string CodeSnippet { get; set; }
        public string Recommendation { get; set; }
        public string Reference { get; set; } // Link to documentation or best practice
    }
}