using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CodeVault.Models;
using CodeVault.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodeVault.Services
{
    public class CodeAnalysisService
    {
        private readonly CodeDbContext _dbContext;
        private readonly ILogger<CodeAnalysisService> _logger;

        public CodeAnalysisService(
            CodeDbContext dbContext,
            ILogger<CodeAnalysisService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // Analyze code complexity
        public async Task<string> AnalyzeComplexityAsync(CodeSnippet codeSnippet)
        {
            try
            {
                _logger.LogInformation($"Analyzing complexity for code snippet ID {codeSnippet.Id}");

                // Check if we have performance metrics already
                var existingMetrics = await _dbContext.CodePerformanceMetrics
                    .Where(m => m.CodeSnippetId == codeSnippet.Id)
                    .ToListAsync();

                var analysis = new List<string>();

                if (existingMetrics.Any())
                {
                    foreach (var metric in existingMetrics)
                    {
                        analysis.Add($"- {metric.MetricName}: {metric.MetricValue}" +
                            (string.IsNullOrEmpty(metric.Notes) ? "" : $" ({metric.Notes})"));
                    }
                }
                else
                {
                    // Perform basic static analysis based on code patterns
                    analysis.AddRange(PerformBasicComplexityAnalysis(codeSnippet.Content, codeSnippet.Language));
                }

                if (!analysis.Any())
                {
                    return "No complexity analysis available for this code snippet.";
                }

                return string.Join("\n", analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing complexity for code snippet ID {codeSnippet.Id}");
                return "Error analyzing code complexity.";
            }
        }

        // Get optimization information for a code snippet
        public async Task<string> GetOptimizationInfoAsync(CodeSnippet codeSnippet)
        {
            try
            {
                _logger.LogInformation($"Getting optimization info for code snippet ID {codeSnippet.Id}");

                var analysis = new List<string>();

                // Check if we have performance metrics
                var performanceMetrics = await _dbContext.CodePerformanceMetrics
                    .Where(m => m.CodeSnippetId == codeSnippet.Id)
                    .ToListAsync();

                if (performanceMetrics.Any())
                {
                    analysis.Add("Performance Metrics:");
                    foreach (var metric in performanceMetrics)
                    {
                        analysis.Add($"- {metric.MetricName}: {metric.MetricValue}");
                        if (!string.IsNullOrEmpty(metric.Notes))
                        {
                            analysis.Add($"  Note: {metric.Notes}");
                        }
                    }
                }

                // Perform basic optimization analysis
                var optimizationSuggestions = GetOptimizationSuggestions(codeSnippet.Content, codeSnippet.Language);
                if (optimizationSuggestions.Any())
                {
                    analysis.Add("\nOptimization Suggestions:");
                    analysis.AddRange(optimizationSuggestions.Select(s => $"- {s}"));
                }

                if (!analysis.Any())
                {
                    return "No optimization information available for this code snippet.";
                }

                return string.Join("\n", analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting optimization info for code snippet ID {codeSnippet.Id}");
                return "Error retrieving optimization information.";
            }
        }

        // Store performance metrics for a code snippet
        public async Task<CodePerformanceMetric> AddPerformanceMetricAsync(
            int codeSnippetId, string metricName, string metricValue,
            double? numericValue = null, string unit = "", string environment = "", string notes = "")
        {
            try
            {
                var codeSnippet = await _dbContext.CodeSnippets.FindAsync(codeSnippetId);
                if (codeSnippet == null)
                {
                    throw new KeyNotFoundException($"Code snippet with ID {codeSnippetId} not found.");
                }

                var metric = new CodePerformanceMetric
                {
                    CodeSnippetId = codeSnippetId,
                    MetricName = metricName,
                    MetricValue = metricValue,
                    NumericValue = numericValue,
                    Unit = unit,
                    Environment = environment,
                    Notes = notes,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.CodePerformanceMetrics.Add(metric);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"Added performance metric '{metricName}' for code snippet ID {codeSnippetId}");

                return metric;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding performance metric for code snippet ID {codeSnippetId}");
                throw;
            }
        }

        // Get performance metrics for a code snippet
        public async Task<List<CodePerformanceMetric>> GetMetricsAsync(int snippetId)
        {
            try
            {
                _logger.LogInformation($"Getting performance metrics for code snippet ID {snippetId}");

                // Get metrics from the database
                var metrics = await _dbContext.CodePerformanceMetrics
                    .Where(m => m.CodeSnippetId == snippetId)
                    .ToListAsync();

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting performance metrics for code snippet ID {snippetId}");
                return new List<CodePerformanceMetric>();
            }
        }

        // Analyze cyclomatic complexity
        public int AnalyzeCyclomaticComplexity(string code, string language)
        {
            try
            {
                int complexity = 1; // Base complexity

                switch (language.ToLower())
                {
                    case "javascript":
                    case "typescript":
                    case "java":
                    case "c#":
                    case "c++":
                    case "php":
                        // Count conditional statements and loops
                        complexity += CountMatches(code, @"if\s*\(");
                        complexity += CountMatches(code, @"else\s+if\s*\(");
                        complexity += CountMatches(code, @"for\s*\(");
                        complexity += CountMatches(code, @"while\s*\(");
                        complexity += CountMatches(code, @"catch\s*\(");
                        complexity += CountMatches(code, @"case\s+[^:]+:");
                        complexity += CountMatches(code, @"\?\s*[^:]+\s*:");  // Ternary operators
                        break;

                    case "python":
                        complexity += CountMatches(code, @"\bif\s+");
                        complexity += CountMatches(code, @"\belif\s+");
                        complexity += CountMatches(code, @"\bfor\s+");
                        complexity += CountMatches(code, @"\bwhile\s+");
                        complexity += CountMatches(code, @"\bexcept\s+");
                        break;

                    case "ruby":
                        complexity += CountMatches(code, @"\bif\s+");
                        complexity += CountMatches(code, @"\belsif\s+");
                        complexity += CountMatches(code, @"\bfor\s+");
                        complexity += CountMatches(code, @"\bwhile\s+");
                        complexity += CountMatches(code, @"\brescue\s+");
                        complexity += CountMatches(code, @"\bcase\s+");
                        break;

                    default:
                        // Generic approach for other languages
                        complexity += CountMatches(code, @"if\s*\(");
                        complexity += CountMatches(code, @"for\s*\(");
                        complexity += CountMatches(code, @"while\s*\(");
                        break;
                }

                return complexity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing cyclomatic complexity");
                return -1; // Error indicator
            }
        }

        // Helper method to count regex matches
        private int CountMatches(string text, string pattern)
        {
            return Regex.Matches(text, pattern).Count;
        }

        // Perform basic complexity analysis
        private List<string> PerformBasicComplexityAnalysis(string code, string language)
        {
            var analysis = new List<string>();

            try
            {
                // Calculate cyclomatic complexity
                int cyclomaticComplexity = AnalyzeCyclomaticComplexity(code, language);
                if (cyclomaticComplexity > 0)
                {
                    analysis.Add($"Cyclomatic Complexity: {cyclomaticComplexity}");

                    // Add interpretation
                    if (cyclomaticComplexity <= 5)
                        analysis.Add("Complexity Level: Low - Code is simple and easy to understand.");
                    else if (cyclomaticComplexity <= 10)
                        analysis.Add("Complexity Level: Moderate - Code has reasonable complexity.");
                    else if (cyclomaticComplexity <= 20)
                        analysis.Add("Complexity Level: High - Consider refactoring for better maintainability.");
                    else
                        analysis.Add("Complexity Level: Very High - Code is complex and may be difficult to maintain.");
                }

                // Analyze nesting depth
                int nestingDepth = AnalyzeNestingDepth(code, language);
                if (nestingDepth > 0)
                {
                    analysis.Add($"Maximum Nesting Depth: {nestingDepth}");

                    if (nestingDepth > 3)
                        analysis.Add("Note: Deep nesting can reduce code readability.");
                }

                // Count functions/methods
                int functionCount = CountFunctions(code, language);
                if (functionCount > 0)
                {
                    analysis.Add($"Function/Method Count: {functionCount}");
                }

                // Analyze code size
                int lineCount = code.Split('\n').Length;
                analysis.Add($"Line Count: {lineCount}");

                if (lineCount > 300)
                    analysis.Add("Note: Large file size may indicate a need to split functionality.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in basic complexity analysis");
            }

            return analysis;
        }

        // Analyze nesting depth
        private int AnalyzeNestingDepth(string code, string language)
        {
            try
            {
                int maxDepth = 0;
                int currentDepth = 0;

                foreach (var line in code.Split('\n'))
                {
                    string trimmedLine = line.Trim();

                    // Increment depth for opening brackets/blocks
                    if (language.ToLower() == "python")
                    {
                        if (trimmedLine.EndsWith(":") &&
                            (trimmedLine.StartsWith("if ") ||
                             trimmedLine.StartsWith("for ") ||
                             trimmedLine.StartsWith("while ") ||
                             trimmedLine.StartsWith("def ") ||
                             trimmedLine.StartsWith("class ")))
                        {
                            currentDepth++;
                        }
                        else if (trimmedLine.StartsWith("return") || trimmedLine == "break" || trimmedLine == "continue")
                        {
                            // Potential end of a block
                            if (currentDepth > 0) currentDepth--;
                        }
                    }
                    else
                    {
                        if (trimmedLine.Contains("{"))
                        {
                            currentDepth += CountCharacter(trimmedLine, '{');
                        }
                        if (trimmedLine.Contains("}"))
                        {
                            currentDepth -= CountCharacter(trimmedLine, '}');
                            if (currentDepth < 0) currentDepth = 0; // Safety check
                        }
                    }

                    maxDepth = Math.Max(maxDepth, currentDepth);
                }

                return maxDepth;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing nesting depth");
                return -1;
            }
        }

        // Count functions/methods
        private int CountFunctions(string code, string language)
        {
            try
            {
                switch (language.ToLower())
                {
                    case "javascript":
                    case "typescript":
                        return CountMatches(code, @"function\s+\w+\s*\(") +
                               CountMatches(code, @"\w+\s*=\s*function\s*\(") +
                               CountMatches(code, @"\w+\s*\([^)]*\)\s*=>") +
                               CountMatches(code, @"\w+\s*:\s*function\s*\(");

                    case "python":
                        return CountMatches(code, @"def\s+\w+\s*\(");

                    case "java":
                    case "c#":
                        return CountMatches(code, @"(?:public|private|protected|internal|static)?\s+\w+\s+\w+\s*\([^)]*\)\s*\{");

                    case "ruby":
                        return CountMatches(code, @"def\s+\w+");

                    default:
                        return CountMatches(code, @"function\s+\w+\s*\(") + CountMatches(code, @"def\s+\w+");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting functions");
                return -1;
            }
        }

        // Count characters in a string
        private int CountCharacter(string text, char character)
        {
            return text.Count(c => c == character);
        }

        // Get optimization suggestions
        private List<string> GetOptimizationSuggestions(string code, string language)
        {
            var suggestions = new List<string>();

            try
            {
                switch (language.ToLower())
                {
                    case "javascript":
                    case "typescript":
                        // Check for inefficient loops
                        if (CountMatches(code, @"for\s*\(\s*var\s+i") > 0)
                        {
                            suggestions.Add("Consider using 'let' instead of 'var' for better scoping.");
                        }

                        // Check for array methods vs loops
                        if (CountMatches(code, @"for\s*\(.*\.length") > 0)
                        {
                            suggestions.Add("Array methods like map(), filter(), reduce() may be more readable than for loops.");
                        }

                        // Check for console.log in production code
                        if (CountMatches(code, @"console\.log\(") > 0)
                        {
                            suggestions.Add("Remove console.log statements for production code.");
                        }
                        break;

                    case "python":
                        // Check for unnecessary list comprehensions
                        if (CountMatches(code, @"\[\s*for\s+") > 0 && CountMatches(code, @"\.append\(") > 0)
                        {
                            suggestions.Add("Consider list comprehensions instead of building lists with append().");
                        }

                        // Check for inefficient string concatenation
                        if (CountMatches(code, "\\\".*?\\\"\\s*\\+\\s*\\\"") > 0)
                        {
                            suggestions.Add("Use f-strings or .format() instead of string concatenation.");
                        }

                        // Check for print statements
                        if (CountMatches(code, @"\bprint\(") > 0)
                        {
                            suggestions.Add("Remove print statements for production code.");
                        }
                        break;

                    case "c#":
                        // Check for string concatenation
                        if (CountMatches(code, @""".*?""\s*\+\s*""") > 0)
                        {
                            suggestions.Add("Consider using string interpolation ($\"\") or StringBuilder for string concatenation.");
                        }

                        // Check for LINQ usage
                        if (CountMatches(code, @"for\s*\(.*\.Length") > 0 || CountMatches(code, @"foreach\s*\(") > 0)
                        {
                            suggestions.Add("LINQ methods might provide more concise solutions than loops.");
                        }
                        break;
                }

                // Generic suggestions
                if (AnalyzeNestingDepth(code, language) > 3)
                {
                    suggestions.Add("High nesting depth detected. Consider refactoring to reduce complexity.");
                }

                if (AnalyzeCyclomaticComplexity(code, language) > 15)
                {
                    suggestions.Add("High cyclomatic complexity detected. Consider breaking down into smaller functions.");
                }

                int lineCount = code.Split('\n').Length;
                if (lineCount > 200)
                {
                    suggestions.Add("File is quite large. Consider splitting into smaller modules.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating optimization suggestions");
            }

            return suggestions;
        }
    }
}