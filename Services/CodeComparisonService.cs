using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeVault.Models;
using Microsoft.Extensions.Logging;

namespace CodeVault.Services
{
    public class CodeComparisonService
    {
        private readonly ILogger<CodeComparisonService> _logger;

        public CodeComparisonService(ILogger<CodeComparisonService> logger)
        {
            _logger = logger;
        }

        // Compare two code snippets
        public async Task<string> CompareSnippetsAsync(CodeSnippet snippet1, CodeSnippet snippet2)
        {
            try
            {
                _logger.LogInformation($"Comparing code snippets: {snippet1.Id} and {snippet2.Id}");

                var comparison = new StringBuilder();

                // Basic metadata comparison
                comparison.AppendLine("# Code Comparison");
                comparison.AppendLine();
                comparison.AppendLine("## Metadata Comparison");
                comparison.AppendLine();
                comparison.AppendLine($"| Feature | {FormatTitle(snippet1.Title)} | {FormatTitle(snippet2.Title)} |");
                comparison.AppendLine("| --- | --- | --- |");
                comparison.AppendLine($"| Language | {snippet1.Language} | {snippet2.Language} |");
                comparison.AppendLine($"| Created | {snippet1.CreatedAt.ToString("yyyy-MM-dd")} | {snippet2.CreatedAt.ToString("yyyy-MM-dd")} |");
                comparison.AppendLine($"| Line Count | {CountLines(snippet1.Content)} | {CountLines(snippet2.Content)} |");
                comparison.AppendLine();

                // Compare tags if they exist
                if (!string.IsNullOrEmpty(snippet1.TagString) || !string.IsNullOrEmpty(snippet2.TagString))
                {
                    var tags1 = snippet1.Tags;
                    var tags2 = snippet2.Tags;

                    comparison.AppendLine("## Tag Comparison");
                    comparison.AppendLine();
                    comparison.AppendLine($"* Snippet 1 Tags: {(tags1.Any() ? string.Join(", ", tags1) : "None")}");
                    comparison.AppendLine($"* Snippet 2 Tags: {(tags2.Any() ? string.Join(", ", tags2) : "None")}");

                    // Find common and unique tags
                    var commonTags = tags1.Intersect(tags2).ToList();
                    var uniqueToSnippet1 = tags1.Except(tags2).ToList();
                    var uniqueToSnippet2 = tags2.Except(tags1).ToList();

                    if (commonTags.Any())
                    {
                        comparison.AppendLine($"* Common Tags: {string.Join(", ", commonTags)}");
                    }

                    if (uniqueToSnippet1.Any())
                    {
                        comparison.AppendLine($"* Tags unique to {FormatTitle(snippet1.Title)}: {string.Join(", ", uniqueToSnippet1)}");
                    }

                    if (uniqueToSnippet2.Any())
                    {
                        comparison.AppendLine($"* Tags unique to {FormatTitle(snippet2.Title)}: {string.Join(", ", uniqueToSnippet2)}");
                    }

                    comparison.AppendLine();
                }

                // Language-specific comparison logic
                if (snippet1.Language == snippet2.Language)
                {
                    comparison.AppendLine("## Language-Specific Analysis");
                    comparison.AppendLine();

                    // Add language-specific comparisons
                    switch (snippet1.Language.ToLower())
                    {
                        case "javascript":
                        case "typescript":
                            comparison.AppendLine(CompareJavaScript(snippet1.Content, snippet2.Content));
                            break;

                        case "python":
                            comparison.AppendLine(ComparePython(snippet1.Content, snippet2.Content));
                            break;

                        case "c#":
                            comparison.AppendLine(CompareCSharp(snippet1.Content, snippet2.Content));
                            break;

                        default:
                            comparison.AppendLine(CompareGeneric(snippet1.Content, snippet2.Content));
                            break;
                    }
                }
                else
                {
                    comparison.AppendLine("## Cross-Language Comparison");
                    comparison.AppendLine();
                    comparison.AppendLine($"* These snippets use different languages ({snippet1.Language} vs {snippet2.Language}).");
                    comparison.AppendLine("* The approaches may differ due to language-specific features and paradigms.");

                    // Add generic comparison
                    comparison.AppendLine(CompareGeneric(snippet1.Content, snippet2.Content));
                }

                // Add a similarity score
                double similarityScore = CalculateSimilarityScore(snippet1.Content, snippet2.Content);
                comparison.AppendLine();
                comparison.AppendLine($"## Overall Similarity Score: {similarityScore:P0}");

                if (similarityScore > 0.8)
                {
                    comparison.AppendLine("These snippets are very similar and likely serve the same purpose with minor variations.");
                }
                else if (similarityScore > 0.5)
                {
                    comparison.AppendLine("These snippets have moderate similarity and may represent different approaches to the same problem.");
                }
                else
                {
                    comparison.AppendLine("These snippets have low similarity and likely represent different approaches or solve different problems.");
                }

                return comparison.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing code snippets");
                return "Error performing code comparison.";
            }
        }

        // Helper to format title for use in markdown table
        private string FormatTitle(string title)
        {
            if (title.Length > 20)
            {
                return title.Substring(0, 17) + "...";
            }
            return title;
        }

        // Count lines in code
        private int CountLines(string code)
        {
            if (string.IsNullOrEmpty(code)) return 0;
            return code.Split('\n').Length;
        }

        // JavaScript/TypeScript comparison
        private string CompareJavaScript(string code1, string code2)
        {
            var comparison = new StringBuilder();

            // Function count
            int funcCount1 = CountPatterns(code1, @"function\s+\w+|const\s+\w+\s*=\s*\(|let\s+\w+\s*=\s*\(|var\s+\w+\s*=\s*\(|\w+\s*=\s*function|\w+\s*\([^)]*\)\s*=>|\bclass\s+\w+");
            int funcCount2 = CountPatterns(code2, @"function\s+\w+|const\s+\w+\s*=\s*\(|let\s+\w+\s*=\s*\(|var\s+\w+\s*=\s*\(|\w+\s*=\s*function|\w+\s*\([^)]*\)\s*=>|\bclass\s+\w+");

            comparison.AppendLine($"* Function/Class count: {funcCount1} vs {funcCount2}");

            // ES6 features usage
            bool usesES6_1 = code1.Contains("=>") || code1.Contains("const ") || code1.Contains("let ");
            bool usesES6_2 = code2.Contains("=>") || code2.Contains("const ") || code2.Contains("let ");

            comparison.AppendLine($"* ES6 Features: {(usesES6_1 ? "Yes" : "No")} vs {(usesES6_2 ? "Yes" : "No")}");

            // Framework detection
            string framework1 = DetectJSFramework(code1);
            string framework2 = DetectJSFramework(code2);

            if (!string.IsNullOrEmpty(framework1) || !string.IsNullOrEmpty(framework2))
            {
                comparison.AppendLine($"* Framework: {framework1 ?? "None"} vs {framework2 ?? "None"}");
            }

            return comparison.ToString();
        }

        // Python comparison
        private string ComparePython(string code1, string code2)
        {
            var comparison = new StringBuilder();

            // Function count
            int funcCount1 = CountPatterns(code1, @"def\s+\w+");
            int funcCount2 = CountPatterns(code2, @"def\s+\w+");

            comparison.AppendLine($"* Function count: {funcCount1} vs {funcCount2}");

            // Class count
            int classCount1 = CountPatterns(code1, @"class\s+\w+");
            int classCount2 = CountPatterns(code2, @"class\s+\w+");

            comparison.AppendLine($"* Class count: {classCount1} vs {classCount2}");

            // Python version features
            bool usesPython3Features1 = code1.Contains("print(") || code1.Contains("__future__") || code1.Contains("f\"");
            bool usesPython3Features2 = code2.Contains("print(") || code2.Contains("__future__") || code2.Contains("f\"");

            comparison.AppendLine($"* Python 3 Specific Features: {(usesPython3Features1 ? "Yes" : "No")} vs {(usesPython3Features2 ? "Yes" : "No")}");

            // Framework detection
            string framework1 = DetectPythonFramework(code1);
            string framework2 = DetectPythonFramework(code2);

            if (!string.IsNullOrEmpty(framework1) || !string.IsNullOrEmpty(framework2))
            {
                comparison.AppendLine($"* Framework: {framework1 ?? "None"} vs {framework2 ?? "None"}");
            }

            return comparison.ToString();
        }

        // C# comparison
        private string CompareCSharp(string code1, string code2)
        {
            var comparison = new StringBuilder();

            // Class count
            int classCount1 = CountPatterns(code1, @"class\s+\w+");
            int classCount2 = CountPatterns(code2, @"class\s+\w+");

            comparison.AppendLine($"* Class count: {classCount1} vs {classCount2}");

            // Method count
            int methodCount1 = CountPatterns(code1, @"(?:public|private|protected|internal|static)?\s+\w+\s+\w+\s*\(");
            int methodCount2 = CountPatterns(code2, @"(?:public|private|protected|internal|static)?\s+\w+\s+\w+\s*\(");

            comparison.AppendLine($"* Method count: {methodCount1} vs {methodCount2}");

            // LINQ usage
            bool usesLinq1 = code1.Contains("using System.Linq") || code1.Contains(".Select(") || code1.Contains(".Where(");
            bool usesLinq2 = code2.Contains("using System.Linq") || code2.Contains(".Select(") || code2.Contains(".Where(");

            comparison.AppendLine($"* LINQ Usage: {(usesLinq1 ? "Yes" : "No")} vs {(usesLinq2 ? "Yes" : "No")}");

            // Async/Await usage
            bool usesAsync1 = code1.Contains("async ") || code1.Contains("await ");
            bool usesAsync2 = code2.Contains("async ") || code2.Contains("await ");

            comparison.AppendLine($"* Async/Await: {(usesAsync1 ? "Yes" : "No")} vs {(usesAsync2 ? "Yes" : "No")}");

            return comparison.ToString();
        }

        // Generic comparison for any language
        private string CompareGeneric(string code1, string code2)
        {
            var comparison = new StringBuilder();

            // Basic structure
            int lineCount1 = CountLines(code1);
            int lineCount2 = CountLines(code2);

            comparison.AppendLine($"* Line Count: {lineCount1} vs {lineCount2}");

            // Comments
            int commentCount1 = CountComments(code1);
            int commentCount2 = CountComments(code2);

            comparison.AppendLine($"* Comment Count: {commentCount1} vs {commentCount2}");

            // Nested Control Structures
            int nestingDepth1 = CountMaxNestingDepth(code1);
            int nestingDepth2 = CountMaxNestingDepth(code2);

            comparison.AppendLine($"* Max Nesting Depth: {nestingDepth1} vs {nestingDepth2}");

            return comparison.ToString();
        }

        // Calculate similarity score between two code snippets
        private double CalculateSimilarityScore(string code1, string code2)
        {
            try
            {
                // Very basic implementation - can be improved
                // Normalize the code first
                string normalized1 = NormalizeCode(code1);
                string normalized2 = NormalizeCode(code2);

                // Split into tokens and compare
                var tokens1 = normalized1.Split(new[] { ' ', '\n', '\t', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                var tokens2 = normalized2.Split(new[] { ' ', '\n', '\t', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                // Count common tokens
                int commonTokens = tokens1.Intersect(tokens2).Count();

                // Calculate Jaccard similarity
                int totalTokens = tokens1.Length + tokens2.Length - commonTokens;
                if (totalTokens == 0) return 1.0;

                return (double)commonTokens / totalTokens;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating similarity score");
                return 0.0;
            }
        }

        // Normalize code for comparison
        private string NormalizeCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return string.Empty;

            // Remove comments
            code = System.Text.RegularExpressions.Regex.Replace(code, @"\/\/.*?$", "", System.Text.RegularExpressions.RegexOptions.Multiline);
            code = System.Text.RegularExpressions.Regex.Replace(code, @"\/\*[\s\S]*?\*\/", "");
            code = System.Text.RegularExpressions.Regex.Replace(code, @"#.*?$", "", System.Text.RegularExpressions.RegexOptions.Multiline);

            // Remove extra whitespace
            code = System.Text.RegularExpressions.Regex.Replace(code, @"\s+", " ");

            // Remove string literals
            code = System.Text.RegularExpressions.Regex.Replace(code, @""".*?""", "\"\"");
            code = System.Text.RegularExpressions.Regex.Replace(code, @"'.*?'", "''");

            return code.Trim();
        }

        // Count patterns in code
        private int CountPatterns(string code, string pattern)
        {
            if (string.IsNullOrEmpty(code)) return 0;
            return System.Text.RegularExpressions.Regex.Matches(code, pattern).Count;
        }

        // Count comments in code
        private int CountComments(string code)
        {
            if (string.IsNullOrEmpty(code)) return 0;

            int count = 0;

            // Count single-line comments
            count += System.Text.RegularExpressions.Regex.Matches(code, @"\/\/.*?$", System.Text.RegularExpressions.RegexOptions.Multiline).Count;
            count += System.Text.RegularExpressions.Regex.Matches(code, @"#.*?$", System.Text.RegularExpressions.RegexOptions.Multiline).Count;

            // Count multi-line comments
            var multiLineMatches = System.Text.RegularExpressions.Regex.Matches(code, @"\/\*[\s\S]*?\*\/");
            foreach (System.Text.RegularExpressions.Match match in multiLineMatches)
            {
                count += match.Value.Split('\n').Length;
            }

            return count;
        }

        // Count max nesting depth
        private int CountMaxNestingDepth(string code)
        {
            if (string.IsNullOrEmpty(code)) return 0;

            int maxDepth = 0;
            int currentDepth = 0;

            foreach (char c in code)
            {
                if (c == '{' || c == '(' || c == '[')
                {
                    currentDepth++;
                    maxDepth = Math.Max(maxDepth, currentDepth);
                }
                else if (c == '}' || c == ')' || c == ']')
                {
                    currentDepth = Math.Max(0, currentDepth - 1);
                }
            }

            return maxDepth;
        }

        // Detect JavaScript framework
        private string DetectJSFramework(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;

            if (code.Contains("React") || code.Contains("jsx") || code.Contains("ReactDOM") || code.Contains("Component"))
                return "React";

            if (code.Contains("Angular") || code.Contains("@Component") || code.Contains("ngOnInit"))
                return "Angular";

            if (code.Contains("Vue") || code.Contains("new Vue") || code.Contains("createApp"))
                return "Vue";

            if (code.Contains("express") || code.Contains("app.get(") || code.Contains("app.use("))
                return "Express.js";

            return null;
        }

        // Detect Python framework
        private string DetectPythonFramework(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;

            if (code.Contains("django") || code.Contains("models.Model") || code.Contains("urls.py"))
                return "Django";

            if (code.Contains("flask") || code.Contains("Flask") || code.Contains("app.route"))
                return "Flask";

            if (code.Contains("tensorflow") || code.Contains("tf.") || code.Contains("keras"))
                return "TensorFlow";

            if (code.Contains("pandas") || code.Contains("pd.") || code.Contains("DataFrame"))
                return "Pandas";

            return null;
        }
    }
}