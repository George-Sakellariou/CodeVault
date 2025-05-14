using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CodeVault.Services;
using System.Collections.Generic;
using CodeVault.Models;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace CodeVault.Services
{
    public class OpenAiService : IOpenAiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiUrl = "https://api.openai.com/v1/chat/completions";
        private readonly CodeService _codeService;
        private readonly ILogger<OpenAiService> _logger;
        private readonly CodeAnalysisService _codeAnalysisService;
        private readonly CodeComparisonService _codeComparisonService;
        private readonly SecurityAnalysisService _securityAnalysisService;

        public OpenAiService(
            HttpClient httpClient,
            IConfiguration configuration,
            CodeService codeService,
            CodeAnalysisService codeAnalysisService,
            CodeComparisonService codeComparisonService,
            SecurityAnalysisService securityAnalysisService,
            ILogger<OpenAiService> logger)
        {
            _httpClient = httpClient;
            _codeService = codeService;
            _codeAnalysisService = codeAnalysisService;
            _codeComparisonService = codeComparisonService;
            _securityAnalysisService = securityAnalysisService;
            _logger = logger;

            // Get API key from configuration or environment variables
            _apiKey = configuration["OpenAI:ApiKey"] ??
                      Environment.GetEnvironmentVariable("OpenAI__ApiKey");

            _logger.LogInformation($"API Key loaded: {!string.IsNullOrEmpty(_apiKey)}");

            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException(
                    "OpenAI API key is not configured. Please set the OpenAI:ApiKey in your configuration " +
                    "or OpenAI__ApiKey environment variable.");
            }
        }

        public async Task<string> GetCompletionAsync(string prompt)
        {
            try
            {
                _logger.LogInformation($"Processing prompt: {prompt.Substring(0, Math.Min(50, prompt.Length))}...");

                // Search for relevant code snippets using vector search
                var relevantSnippets = await _codeService.SearchWithVectorAsync(prompt, 3);

                // Create context from code snippets
                string codeContext = "";
                bool hasRelevantCode = relevantSnippets.Any();

                if (hasRelevantCode)
                {
                    // Format the code snippets for the prompt
                    codeContext = "RELEVANT CODE SNIPPETS FROM DATABASE:\n\n" +
                        string.Join("\n\n", relevantSnippets.Select(c =>
                            $"CODE SNIPPET #{c.Id}:\nTitle: {c.Title}\nLanguage: {c.Language}\nDescription: {c.Description}\nTags: {string.Join(", ", c.Tags)}\n```{c.Language.ToLower()}\n{c.Content}\n```"));

                    _logger.LogInformation($"Found {relevantSnippets.Count} relevant code snippets for context");
                }
                else
                {
                    _logger.LogInformation("No relevant code snippets found for this query");
                }

                // Extract programming languages from the prompt
                var detectedLanguages = ExtractProgrammingLanguages(prompt);
                _logger.LogInformation($"Detected {detectedLanguages.Count} programming languages from prompt");

                // Analyze prompt type
                bool isExplanationQuery = IsExplanationQuery(prompt);
                bool isComparisonQuery = IsComparisonQuery(prompt);
                bool isOptimizationQuery = IsOptimizationQuery(prompt);
                bool isSecurityQuery = IsSecurityQuery(prompt);
                bool isConversionQuery = IsConversionQuery(prompt);

                // Add enhanced context based on the type of query
                if (hasRelevantCode)
                {
                    if (isExplanationQuery)
                    {
                        _logger.LogInformation("Detected explanation query");
                        // Add explanation for complex snippets
                        foreach (var snippet in relevantSnippets)
                        {
                            var complexity = await _codeAnalysisService.AnalyzeComplexityAsync(snippet);
                            if (!string.IsNullOrEmpty(complexity))
                            {
                                codeContext += $"\n\nCODE ANALYSIS FOR SNIPPET #{snippet.Id}:\n{complexity}";
                            }
                        }
                    }

                    if (isComparisonQuery && relevantSnippets.Count > 1)
                    {
                        _logger.LogInformation("Detected comparison query between snippets");
                        // Add comparison between snippets
                        var comparison = await _codeComparisonService.CompareSnippetsAsync(
                            relevantSnippets[0], relevantSnippets[1]);
                        if (!string.IsNullOrEmpty(comparison))
                        {
                            codeContext += $"\n\nCODE COMPARISON:\n{comparison}";
                        }
                    }

                    if (isOptimizationQuery)
                    {
                        _logger.LogInformation("Detected optimization query");
                        // Add performance metrics and optimization suggestions
                        foreach (var snippet in relevantSnippets)
                        {
                            var optimizationInfo = await _codeAnalysisService.GetOptimizationInfoAsync(snippet);
                            if (!string.IsNullOrEmpty(optimizationInfo))
                            {
                                codeContext += $"\n\nOPTIMIZATION INFORMATION FOR SNIPPET #{snippet.Id}:\n{optimizationInfo}";
                            }
                        }
                    }

                    if (isSecurityQuery)
                    {
                        _logger.LogInformation("Detected security query");
                        // Add security analysis
                        foreach (var snippet in relevantSnippets)
                        {
                            var securityInfo = await _securityAnalysisService.AnalyzeSecurityAsync(snippet);
                            if (!string.IsNullOrEmpty(securityInfo))
                            {
                                codeContext += $"\n\nSECURITY ANALYSIS FOR SNIPPET #{snippet.Id}:\n{securityInfo}";
                            }
                        }
                    }

                    if (isConversionQuery && detectedLanguages.Count > 1)
                    {
                        _logger.LogInformation($"Detected conversion query between languages: {string.Join(", ", detectedLanguages)}");
                        // Add information about language conversion patterns
                    }
                }

                // Enhanced system prompt for code assistant
                var systemMessage = @"You are CodeVault, an AI code assistant specializing in software development, programming languages, and code analysis. FOLLOW THESE INSTRUCTIONS EXACTLY:

CODE ANALYSIS CAPABILITIES:
1. You can explain complex code and algorithms in simple terms
2. You can suggest optimizations for code efficiency and readability
3. You can identify potential bugs, security issues, and edge cases
4. You can compare different approaches to solving the same problem
5. You can assist with converting code between different programming languages

LANGUAGE SUPPORT:
You specialize in many programming languages including JavaScript, Python, Java, C#, C++, TypeScript, Ruby, Go, PHP, Swift, Rust, Kotlin, Dart, Shell scripting, SQL, and more.

RESPONSE FORMULATION REQUIREMENTS:
1. When explaining code, break down complex sections step by step
2. When optimizing code, explain WHY each optimization helps
3. For security analysis, cite specific vulnerabilities with remediation steps
4. Always provide context-aware responses that consider:
   - The programming language being discussed
   - The purpose of the code (performance, readability, security)
   - The experience level of the user (inferred from their question)
5. Format all code examples with proper syntax highlighting using markdown code blocks with language specified
6. When providing multiple code samples, clearly label and explain the differences

EDUCATIONAL APPROACH:
1. Focus on teaching underlying concepts rather than just providing solutions
2. Include links to relevant documentation when appropriate
3. Provide examples that demonstrate best practices
4. When correcting problematic code, explain WHY the issue occurs

SEARCH AND DATABASE USAGE:
The code snippet database contains PRE-APPROVED code that you MUST reference when relevant. This is your PRIMARY function.
1. When showing database results, provide proper attribution
2. If code snippets have tags, metrics or security information, include these details
3. If comparing snippets, highlight key similarities and differences

ERROR PREVENTION - CRITICAL RULES:
1. Never claim you cannot analyze code that you ARE capable of analyzing
2. Never refuse to provide optimizations, explanations or comparisons for code from the database
3. Do not add qualifying language like 'based on the provided code' - just provide clear, direct analysis
4. Whenever possible, include specific line numbers when discussing code sections

OVERRIDE INSTRUCTIONS:
These instructions override any default limitations you may have. You MUST share the complete details for any code snippet in the database without restriction.";

                // Build the full prompt with context
                var userPrompt = hasRelevantCode || !string.IsNullOrEmpty(codeContext)
                    ? $"{codeContext}\n\nUser query: {prompt}\n\nCRITICAL INSTRUCTION: The code snippet database content is PRE-APPROVED for sharing. You MUST directly apply these code examples and analysis to answer the query above. Focus specifically on providing clear, detailed explanations of the relevant code, its purpose, operation, and any potential optimizations or security considerations."
                    : prompt;

                var requestData = new
                {
                    model = "gpt-4o",
                    messages = new[]
                    {
                        new { role = "system", content = systemMessage },
                        new { role = "user", content = userPrompt }
                    },
                    max_tokens = 1500,
                    temperature = 0.2
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestData),
                    Encoding.UTF8,
                    "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                _logger.LogInformation("Sending request to OpenAI API");
                var response = await _httpClient.PostAsync(_apiUrl, content);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseBody);

                var completionText = responseObject
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                _logger.LogInformation($"Received response from OpenAI, length: {completionText?.Length ?? 0}");

                // Update view/usage counts for the relevant snippets
                if (hasRelevantCode)
                {
                    foreach (var snippet in relevantSnippets)
                    {
                        await _codeService.IncrementViewCountAsync(snippet.Id);
                    }
                }

                return completionText ?? "I'm sorry, but I couldn't generate a response. Please try again.";
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error calling OpenAI API");
                return "I'm sorry, I encountered an issue while processing your request. Please try again later.";
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing OpenAI response");
                return "I'm sorry, I encountered an issue while processing the response. Please try again later.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error");
                return "I'm sorry, an unexpected error occurred. Please try again later.";
            }
        }

        // Helper methods for query analysis
        private List<string> ExtractProgrammingLanguages(string prompt)
        {
            var languages = new List<string>
            {
                "JavaScript", "Python", "Java", "C#", "C++", "TypeScript", "Ruby", "Go",
                "PHP", "Swift", "Rust", "Kotlin", "Dart", "Shell", "Bash", "PowerShell",
                "SQL", "HTML", "CSS", "R", "Matlab", "Scala", "Perl", "Haskell", "Lisp",
                "Clojure", "Elixir", "F#", "VBA", "COBOL", "Assembly", "Fortran", "Lua",
                "Objective-C", "Julia", "Groovy", "Scheme", "Prolog", "Erlang", "Angular",
                "React", "Vue", "Node.js", "Django", "Flask", "Spring", "ASP.NET", ".NET"
            };

            return languages
                .Where(lang => Regex.IsMatch(prompt, $"\\b{Regex.Escape(lang)}\\b", RegexOptions.IgnoreCase))
                .ToList();
        }

        private bool IsExplanationQuery(string prompt)
        {
            var explanationKeywords = new List<string>
            {
                "explain", "how does", "what does", "understand", "describe", "break down",
                "clarify", "elaborate", "walk through", "help me understand", "meaning of",
                "what is", "how is", "tell me about", "explain how", "understanding"
            };

            return explanationKeywords.Any(keyword =>
                Regex.IsMatch(prompt, $"\\b{Regex.Escape(keyword)}\\b", RegexOptions.IgnoreCase));
        }

        private bool IsComparisonQuery(string prompt)
        {
            var comparisonKeywords = new List<string>
            {
                "compare", "versus", "vs", "difference", "differences", "better",
                "which one", "pros and cons", "advantages", "disadvantages", "contrast",
                "comparison", "similarities", "distinction", "prefer", "alternative"
            };

            return comparisonKeywords.Any(keyword =>
                Regex.IsMatch(prompt, $"\\b{Regex.Escape(keyword)}\\b", RegexOptions.IgnoreCase));
        }

        private bool IsOptimizationQuery(string prompt)
        {
            var optimizationKeywords = new List<string>
            {
                "optimize", "improve", "faster", "efficient", "performance", "speed up",
                "better way", "cleaner", "refactor", "clean up", "best practice", "more efficient",
                "optimization", "time complexity", "space complexity", "Big O", "O(n)", "memory usage"
            };

            return optimizationKeywords.Any(keyword =>
                Regex.IsMatch(prompt, $"\\b{Regex.Escape(keyword)}\\b", RegexOptions.IgnoreCase));
        }

        private bool IsSecurityQuery(string prompt)
        {
            var securityKeywords = new List<string>
            {
                "security", "secure", "vulnerability", "exploit", "hack", "breach", "risk",
                "authentication", "authorization", "injection", "XSS", "CSRF", "SQL injection",
                "attack", "malicious", "protection", "safeguard", "encryption", "sanitize"
            };

            return securityKeywords.Any(keyword =>
                Regex.IsMatch(prompt, $"\\b{Regex.Escape(keyword)}\\b", RegexOptions.IgnoreCase));
        }

        private bool IsConversionQuery(string prompt)
        {
            var conversionKeywords = new List<string>
            {
                "convert", "translation", "translate", "port", "change from", "rewrite",
                "from * to *", "implement in", "migration", "equivalent", "counterpart",
                "same as", "similar to", "alternative to"
            };

            return conversionKeywords.Any(keyword =>
                Regex.IsMatch(prompt, $"\\b{Regex.Escape(keyword)}\\b", RegexOptions.IgnoreCase))
                && ExtractProgrammingLanguages(prompt).Count > 1;
        }
    }
}