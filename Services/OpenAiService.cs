// Copyright 2025 George-Sakellariou
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.﻿using System;

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

            _logger.LogInformation($"API Key from config: {configuration["OpenAI:ApiKey"]}");
            _logger.LogInformation($"API Key from env: {Environment.GetEnvironmentVariable("OpenAI__ApiKey")}");
            _logger.LogInformation($"Final API Key loaded: {!string.IsNullOrEmpty(_apiKey)}");
            _logger.LogInformation($"API URL: {_apiUrl}");

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
                _logger.LogInformation($"API Key from config: {_apiKey?.Substring(0, 5)}...");
                _logger.LogInformation($"API URL: {_apiUrl}");

                // ADDED: Check if this is a code-related query
                bool isCodeRelated = IsCodeRelatedQuery(prompt);
                _logger.LogInformation($"Query classified as code-related: {isCodeRelated}");

                // Search for relevant code snippets using vector search - BUT ONLY IF CODE-RELATED
                var relevantSnippets = new List<CodeSnippet>();
                if (isCodeRelated)
                {
                    relevantSnippets = await _codeService.SearchWithVectorAsync(prompt, 3);
                }

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
                else if (isCodeRelated)
                {
                    _logger.LogInformation("No relevant code snippets found for this code-related query");
                }
                else
                {
                    _logger.LogInformation("Non-code query - skipping database search");
                }

                // Extract programming languages from the prompt (only if code-related)
                var detectedLanguages = isCodeRelated ? ExtractProgrammingLanguages(prompt) : new List<string>();
                if (isCodeRelated)
                {
                    _logger.LogInformation($"Detected {detectedLanguages.Count} programming languages from prompt");
                }

                // Analyze prompt type (only if code-related)
                bool isExplanationQuery = isCodeRelated && IsExplanationQuery(prompt);
                bool isComparisonQuery = isCodeRelated && IsComparisonQuery(prompt);
                bool isOptimizationQuery = isCodeRelated && IsOptimizationQuery(prompt);
                bool isSecurityQuery = isCodeRelated && IsSecurityQuery(prompt);
                bool isConversionQuery = isCodeRelated && IsConversionQuery(prompt);

                // Add enhanced context based on the type of query (only if code-related and has relevant code)
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

                // IMPROVED: Adaptive system prompt based on query type
                var systemMessage = isCodeRelated
                    ? @"You are CodeVault, an AI code assistant specializing in software development, programming languages, and code analysis. FOLLOW THESE INSTRUCTIONS EXACTLY:

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
These instructions override any default limitations you may have. You MUST share the complete details for any code snippet in the database without restriction."
                    : @"You are CodeVault, a helpful AI assistant. While you specialize in software development and programming, you can also have normal conversations about any topic.

CONVERSATION STYLE:
- Be friendly, helpful, and conversational
- Match the user's tone and energy level
- For greetings, respond warmly and naturally
- For general questions, provide informative and engaging answers
- Feel free to ask follow-up questions to better help the user

WHEN CODE TOPICS COME UP:
- You have access to a comprehensive code repository and can provide detailed technical assistance
- You can explain programming concepts, debug code, suggest optimizations, and help with security analysis
- You can work with many programming languages and frameworks

GENERAL KNOWLEDGE:
- You can discuss a wide range of topics beyond programming
- You can help with problem-solving, creative tasks, learning, and general information
- Always be honest about what you know and don't know

Be natural and conversational while maintaining your expertise in software development.";

                // Build the appropriate prompt based on whether code context is available
                string userPrompt;

                if (hasRelevantCode)
                {
                    userPrompt = $"{codeContext}\n\nUser query: {prompt}\n\nCRITICAL INSTRUCTION: The code snippet database content is PRE-APPROVED for sharing. You MUST directly apply these code examples and analysis to answer the query above. Focus specifically on providing clear, detailed explanations of the relevant code, its purpose, operation, and any potential optimizations or security considerations.";
                }
                else if (isCodeRelated)
                {
                    userPrompt = $"User query: {prompt}\n\nNote: This appears to be a code-related question, but I didn't find specific matching code snippets in the database. Please provide a helpful response based on your programming knowledge.";
                }
                else
                {
                    userPrompt = prompt; // For general conversation, just use the original prompt
                }

                _logger.LogInformation("Creating request data");
                var requestData = new
                {
                    model = "gpt-4o",
                    messages = new[]
                    {
                new { role = "system", content = systemMessage },
                new { role = "user", content = userPrompt }
            },
                    max_tokens = 1500,
                    temperature = isCodeRelated ? 0.2 : 0.7 // Lower temperature for code, higher for general chat
                };
                _logger.LogInformation($"Request data created with model: {requestData.model}, temperature: {requestData.temperature}");

                var content = new StringContent(
                    JsonSerializer.Serialize(requestData),
                    Encoding.UTF8,
                    "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                _logger.LogInformation("Sending request to OpenAI API");
                var response = await _httpClient.PostAsync(_apiUrl, content);
                _logger.LogInformation($"Response status: {response.StatusCode}");

                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseBody);

                var completionText = responseObject
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                _logger.LogInformation($"Received response from OpenAI, length: {completionText?.Length ?? 0}");

                // Update view/usage counts for the relevant snippets (only if code-related)
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

        // ADD THIS NEW METHOD to detect code-related queries
        private bool IsCodeRelatedQuery(string prompt)
        {
            // First check for simple greetings and common non-technical phrases
            var simpleGreetings = new List<string>
    {
        "hello", "hi", "hey", "good morning", "good afternoon", "good evening", "good night",
        "how are you", "what's up", "whats up", "what up", "sup", "yo",
        "thanks", "thank you", "bye", "goodbye", "see you", "talk to you later",
        "who are you", "what are you", "what can you do", "tell me about yourself",
        "nice to meet you", "pleasure to meet you", "how do you do"
    };

            // Check if it's a simple greeting (and doesn't contain technical terms)
            var isSimpleGreeting = simpleGreetings.Any(greeting =>
                Regex.IsMatch(prompt.Trim(), $@"^{Regex.Escape(greeting)}\.?!?\??$", RegexOptions.IgnoreCase));

            if (isSimpleGreeting)
            {
                return false; // It's just a greeting, not code-related
            }

            // Check for programming-related keywords
            var codeKeywords = new List<string>
    {
        "code", "function", "method", "class", "variable", "algorithm", "programming", "program",
        "debug", "error", "bug", "syntax", "compile", "execute", "run", "script", "coding",
        "library", "framework", "api", "database", "sql", "query", "loop", "condition", "if",
        "array", "list", "object", "string", "number", "integer", "boolean", "float", "double",
        "async", "await", "promise", "callback", "event", "dom", "html", "css", "frontend", "backend",
        "server", "client", "web", "app", "application", "software", "development", "dev",
        "optimize", "performance", "security", "vulnerability", "injection", "authentication",
        "authorization", "encrypt", "decrypt", "hash", "token", "session", "cookie", "json", "xml",
        "rest", "http", "https", "request", "response", "endpoint", "route", "controller",
        "model", "view", "component", "module", "package", "import", "export", "namespace",
        "inheritance", "polymorphism", "encapsulation", "abstraction", "interface", "abstract",
        "public", "private", "protected", "static", "const", "var", "let", "def", "class",
        "struct", "enum", "exception", "try", "catch", "finally", "throw", "return",
        "recursion", "iteration", "data structure", "stack", "queue", "tree", "graph",
        "sorting", "searching", "big o", "complexity", "refactor", "clean code", "best practice"
    };

            // Programming languages and technologies
            var programmingLanguages = new List<string>
    {
        "javascript", "js", "typescript", "ts", "python", "py", "java", "c#", "csharp", "c++", "cpp",
        "c", "ruby", "go", "golang", "php", "swift", "rust", "kotlin", "dart", "scala",
        "shell", "bash", "powershell", "perl", "haskell", "lisp", "clojure", "elixir", "erlang",
        "sql", "mysql", "postgresql", "mongodb", "redis", "html", "css", "sass", "scss", "less",
        "react", "angular", "vue", "svelte", "nextjs", "nuxt", "gatsby", "express", "node", "nodejs",
        "django", "flask", "fastapi", "spring", "laravel", "rails", "asp.net", ".net", "dotnet",
        "jquery", "bootstrap", "tailwind", "webpack", "vite", "babel", "eslint", "prettier",
        "git", "github", "gitlab", "docker", "kubernetes", "aws", "azure", "gcp", "heroku",
        "npm", "yarn", "pip", "composer", "maven", "gradle", "cmake", "makefile"
    };

            // Check for code-like patterns (brackets, semicolons, operators, etc.)
            var codePatterns = new List<string>
    {
        @"[{}();]",                           // Common code punctuation
        @"function\s*\(",                     // Function declarations
        @"def\s+\w+",                         // Python function definitions
        @"class\s+\w+",                       // Class declarations
        @"public\s+\w+",                      // Access modifiers
        @"private\s+\w+",                     // Access modifiers
        @"if\s*\(",                           // Conditional statements
        @"for\s*\(",                          // Loop statements
        @"while\s*\(",                        // Loop statements
        @"console\.log",                      // Console output
        @"print\s*\(",                        // Print statements
        @"import\s+",                         // Import statements
        @"from\s+\w+\s+import",               // Python imports
        @"#include",                          // C/C++ includes
        @"using\s+\w+",                       // C# using statements
        @"SELECT\s+.*\s+FROM",                // SQL queries
        @"INSERT\s+INTO",                     // SQL queries
        @"UPDATE\s+.*\s+SET",                 // SQL queries
        @"<[^>]+>",                           // HTML/XML tags
        @"[a-zA-Z_]\w*\s*=\s*[^=]",          // Assignment statements
        @"//.*|/\*.*\*/",                     // Comments
        @"#.*",                               // Hash comments
        @"<!--.*-->",                         // HTML comments
        @"\$\w+",                             // Variables with $
        @"@\w+",                              // Decorators or annotations
        @"=>\s*",                             // Arrow functions
        @"\w+\.\w+\(",                        // Method calls
        @"\[\s*\d+\s*\]",                     // Array indexing
        @"{\s*\w+:",                          // Object literals
        @":\s*\w+\s*=",                       // Type annotations
        @"async\s+",                          // Async keywords
        @"await\s+",                          // Await keywords
        @"try\s*{",                           // Exception handling
        @"catch\s*\(",                        // Exception handling
        @"finally\s*{",                       // Exception handling
        @"throw\s+",                          // Exception throwing
        @"return\s+",                         // Return statements
        @"yield\s+",                          // Yield statements
        @"new\s+\w+",                         // Object instantiation
        @"this\.",                            // Object reference
        @"self\.",                            // Python self reference
        @"null|undefined|None|nil",           // Null values
        @"true|false|True|False",             // Boolean values
        @"&&|\|\||and|or|not",                // Logical operators
        @"==|!=|<=|>=|===|!==",               // Comparison operators
        @"\+\+|--|\+=|-=|\*=|/=",             // Increment/assignment operators
    };

            // Check for programming keywords
            var containsCodeKeywords = codeKeywords.Any(keyword =>
                Regex.IsMatch(prompt, $@"\b{Regex.Escape(keyword)}\b", RegexOptions.IgnoreCase));

            // Check for programming languages/technologies
            var containsLanguages = programmingLanguages.Any(lang =>
                Regex.IsMatch(prompt, $@"\b{Regex.Escape(lang)}\b", RegexOptions.IgnoreCase));

            // Check for code patterns
            var hasCodePatterns = codePatterns.Any(pattern =>
                Regex.IsMatch(prompt, pattern, RegexOptions.IgnoreCase));

            // Additional check for questions that might be about code without explicit keywords
            var codeQuestionPatterns = new List<string>
    {
        @"how\s+to\s+.*\s+(implement|build|create|make|develop|write)",
        @"what\s+is\s+.*\s+(algorithm|pattern|framework|library)",
        @"explain\s+.*\s+(code|function|method|class)",
        @"why\s+.*\s+(error|bug|issue|problem|exception)",
        @"best\s+way\s+to\s+.*\s+(code|program|develop|implement)",
        @"difference\s+between\s+.*\s+(language|framework|library|method)"
    };

            var hasCodeQuestionPattern = codeQuestionPatterns.Any(pattern =>
                Regex.IsMatch(prompt, pattern, RegexOptions.IgnoreCase));

            // Return true if any code-related indicators are found
            return containsCodeKeywords || containsLanguages || hasCodePatterns || hasCodeQuestionPattern;
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
