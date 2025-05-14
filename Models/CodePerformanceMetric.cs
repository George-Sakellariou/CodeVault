using System;

namespace CodeVault.Models
{
    public class CodePerformanceMetric
    {
        public int Id { get; set; }
        public int CodeSnippetId { get; set; }
        public CodeSnippet CodeSnippet { get; set; }

        public string MetricName { get; set; }  // e.g., "Time Complexity", "Space Complexity", "Execution Time"
        public string MetricValue { get; set; } // e.g., "O(n)", "O(1)", "250ms"

        public double? NumericValue { get; set; }
        public string Unit { get; set; } = string.Empty; // e.g., "ms", "MB", "ops/sec"

        public string Environment { get; set; } = string.Empty; // e.g., "Node.js 18.x", "Python 3.10", ".NET 8.0"
        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}