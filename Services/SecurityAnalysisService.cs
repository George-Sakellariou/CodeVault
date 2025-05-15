using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CodeVault.Data;
using CodeVault.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodeVault.Services
{
    public class SecurityAnalysisService
    {
        private readonly CodeDbContext _dbContext;
        private readonly ILogger<SecurityAnalysisService> _logger;

        public SecurityAnalysisService(
            CodeDbContext dbContext,
            ILogger<SecurityAnalysisService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // Analyze security issues in code
        public async Task<string> AnalyzeSecurityAsync(CodeSnippet codeSnippet)
        {
            try
            {
                _logger.LogInformation($"Analyzing security for code snippet ID {codeSnippet.Id}");

                // Check if we have security scans already
                var existingScans = await _dbContext.CodeSecurityScans
                    .Where(s => s.CodeSnippetId == codeSnippet.Id)
                    .OrderByDescending(s => s.ScanDate)
                    .Take(1)
                    .ToListAsync();

                if (existingScans.Any())
                {
                    var scan = existingScans.First();
                    return FormatSecurityScanResults(scan);
                }
                else
                {
                    // Perform basic security analysis
                    var securityIssues = PerformBasicSecurityAnalysis(codeSnippet.Content, codeSnippet.Language);

                    if (!securityIssues.Any())
                    {
                        return "No security issues detected in this code snippet.";
                    }

                    // Create a new security scan record
                    int critical = securityIssues.Count(i => i.Severity == "Critical");
                    int high = securityIssues.Count(i => i.Severity == "High");
                    int medium = securityIssues.Count(i => i.Severity == "Medium");
                    int low = securityIssues.Count(i => i.Severity == "Low");

                    var scan = new CodeSecurityScan
                    {
                        CodeSnippetId = codeSnippet.Id,
                        ScanDate = DateTime.UtcNow,
                        Scanner = "Basic Analysis",
                        SecurityScore = CalculateSecurityScore(critical, high, medium, low),
                        CriticalIssues = critical,
                        HighIssues = high,
                        MediumIssues = medium,
                        LowIssues = low,
                        Findings = securityIssues
                    };

                    _dbContext.CodeSecurityScans.Add(scan);
                    await _dbContext.SaveChangesAsync();

                    return FormatSecurityScanResults(scan);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing security for code snippet ID {codeSnippet.Id}");
                return "Error analyzing code security.";
            }
        }

        // Perform a security scan and store results
        public async Task<CodeSecurityScan> PerformSecurityScanAsync(int codeSnippetId)
        {
            try
            {
                var codeSnippet = await _dbContext.CodeSnippets.FindAsync(codeSnippetId);
                if (codeSnippet == null)
                {
                    throw new KeyNotFoundException($"Code snippet with ID {codeSnippetId} not found.");
                }

                _logger.LogInformation($"Performing security scan for code snippet ID {codeSnippetId}");

                // Perform security analysis
                var securityIssues = PerformBasicSecurityAnalysis(codeSnippet.Content, codeSnippet.Language);

                // Calculate statistics
                int critical = securityIssues.Count(i => i.Severity == "Critical");
                int high = securityIssues.Count(i => i.Severity == "High");
                int medium = securityIssues.Count(i => i.Severity == "Medium");
                int low = securityIssues.Count(i => i.Severity == "Low");

                // Create a new security scan record
                var scan = new CodeSecurityScan
                {
                    CodeSnippetId = codeSnippetId,
                    ScanDate = DateTime.UtcNow,
                    Scanner = "Basic Analysis",
                    SecurityScore = CalculateSecurityScore(critical, high, medium, low),
                    CriticalIssues = critical,
                    HighIssues = high,
                    MediumIssues = medium,
                    LowIssues = low,
                    Findings = securityIssues
                };

                _dbContext.CodeSecurityScans.Add(scan);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"Security scan completed for code snippet ID {codeSnippetId}, found {securityIssues.Count} issues");

                return scan;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error performing security scan for code snippet ID {codeSnippetId}");
                throw;
            }
        }

        // Format security scan results for display
        private string FormatSecurityScanResults(CodeSecurityScan scan)
        {
            var result = new StringBuilder();

            result.AppendLine("## Security Scan Results");
            result.AppendLine();
            result.AppendLine($"Scan Date: {scan.ScanDate.ToString("yyyy-MM-dd HH:mm")}");
            result.AppendLine($"Scanner: {scan.Scanner}");

            if (scan.SecurityScore.HasValue)
            {
                result.AppendLine($"Security Score: {scan.SecurityScore}/100");
            }

            result.AppendLine();
            result.AppendLine("### Issues Summary");
            result.AppendLine();
            result.AppendLine($"* Critical: {scan.CriticalIssues}");
            result.AppendLine($"* High: {scan.HighIssues}");
            result.AppendLine($"* Medium: {scan.MediumIssues}");
            result.AppendLine($"* Low: {scan.LowIssues}");

            if (scan.Findings.Any())
            {
                result.AppendLine();
                result.AppendLine("### Detailed Findings");
                result.AppendLine();

                foreach (var finding in scan.Findings)
                {
                    result.AppendLine($"#### {finding.Severity}: {finding.Title}");
                    result.AppendLine();
                    result.AppendLine(finding.Description);

                    if (!string.IsNullOrEmpty(finding.CodeSnippet))
                    {
                        result.AppendLine();
                        result.AppendLine("```");
                        result.AppendLine(finding.CodeSnippet);
                        result.AppendLine("```");
                    }

                    if (!string.IsNullOrEmpty(finding.Recommendation))
                    {
                        result.AppendLine();
                        result.AppendLine($"**Recommendation**: {finding.Recommendation}");
                    }

                    if (!string.IsNullOrEmpty(finding.Reference))
                    {
                        result.AppendLine();
                        result.AppendLine($"**Reference**: {finding.Reference}");
                    }

                    result.AppendLine();
                }
            }
            else
            {
                result.AppendLine();
                result.AppendLine("No security issues were found in this code snippet.");
            }

            return result.ToString();
        }

        // Calculate security score based on issues
        private int CalculateSecurityScore(int critical, int high, int medium, int low)
        {
            // Base score of 100
            int score = 100;

            // Deduct more points for each issue type
            score -= critical * 25;  
            score -= high * 15;      
            score -= medium * 8;     
            score -= low * 4;        

            // Ensure score is between 0 and 100
            return Math.Max(0, Math.Min(100, score));
        }

        // Perform basic security analysis based on language
        private List<SecurityFinding> PerformBasicSecurityAnalysis(string code, string language)
        {
            var findings = new List<SecurityFinding>();

            try
            {
                // Get common vulnerabilities for all languages
                findings.AddRange(CheckForHardcodedCredentials(code));
                findings.AddRange(CheckForInsecureRandomness(code));

                // Language-specific security checks
                switch (language.ToLower())
                {
                    case "javascript":
                    case "typescript":
                        findings.AddRange(CheckJavaScriptSecurity(code));
                        break;

                    case "python":
                        findings.AddRange(CheckPythonSecurity(code));
                        break;

                    case "c#":
                        findings.AddRange(CheckCSharpSecurity(code));
                        break;

                    case "php":
                        findings.AddRange(CheckPHPSecurity(code));
                        break;

                    case "java":
                        findings.AddRange(CheckJavaSecurity(code));
                        break;

                    case "sql":
                        findings.AddRange(CheckSQLSecurity(code));
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in basic security analysis");
            }

            return findings;
        }

        // Check for hardcoded credentials
        private List<SecurityFinding> CheckForHardcodedCredentials(string code)
        {
            var findings = new List<SecurityFinding>();

            // Check for potential API keys
            var apiKeyMatches = Regex.Matches(code, @"(?:api|access|secret|auth|jwt|token|key)[\w_\-]*\s*(?:=|:)\s*['""]([A-Za-z0-9_\-\.=]{8,})['""]", RegexOptions.IgnoreCase);

            foreach (Match match in apiKeyMatches)
            {
                findings.Add(new SecurityFinding
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Hardcoded API Key",
                    Severity = "High",
                    Description = "Hardcoded API keys pose a security risk if the code is shared or exposed. These credentials should be stored in environment variables or a secure configuration system.",
                    LineNumber = GetLineNumber(code, match.Index),
                    CodeSnippet = GetContextSnippet(code, match.Index),
                    Recommendation = "Move API keys to environment variables or a secure secret management system.",
                    Reference = "https://owasp.org/www-community/vulnerabilities/Hardcoded_credentials"
                });
            }

            // Check for passwords
            var passwordMatches = Regex.Matches(code, @"(?:password|passwd|pwd|secret|credentials)[\w_\-]*\s*(?:=|:)\s*['""]([^'""]{4,})['""]", RegexOptions.IgnoreCase);

            foreach (Match match in passwordMatches)
            {
                findings.Add(new SecurityFinding
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Hardcoded Password",
                    Severity = "Critical",
                    Description = "Hardcoded passwords are a serious security vulnerability. Passwords should never be stored in source code.",
                    LineNumber = GetLineNumber(code, match.Index),
                    CodeSnippet = GetContextSnippet(code, match.Index),
                    Recommendation = "Use environment variables, secure credential stores, or identity management systems instead of hardcoding passwords.",
                    Reference = "https://owasp.org/www-community/vulnerabilities/Hardcoded_credentials"
                });
            }

            return findings;
        }

        // Check for insecure randomness
        private List<SecurityFinding> CheckForInsecureRandomness(string code)
        {
            var findings = new List<SecurityFinding>();

            // Check for insecure random number generation
            var randomMatches = Regex.Matches(code, @"\b(?:Math\.random\(\)|random\.random\(\)|new Random\(\))\b");

            foreach (Match match in randomMatches)
            {
                findings.Add(new SecurityFinding
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Insecure Randomness",
                    Severity = "Medium",
                    Description = "Using standard random number generators for security-sensitive operations is unsafe. These generators are not cryptographically secure.",
                    LineNumber = GetLineNumber(code, match.Index),
                    CodeSnippet = GetContextSnippet(code, match.Index),
                    Recommendation = "Use cryptographically secure random number generators for security operations.",
                    Reference = "https://owasp.org/www-community/vulnerabilities/Insecure_Randomness"
                });
            }

            return findings;
        }

        // Get the latest security scan for a code snippet
        public async Task<CodeSecurityScan> GetLatestScanAsync(int snippetId)
        {
            try
            {
                _logger.LogInformation($"Getting latest security scan for code snippet ID {snippetId}");

                // Get the latest scan from the database
                var scan = await _dbContext.CodeSecurityScans
                    .Where(s => s.CodeSnippetId == snippetId)
                    .OrderByDescending(s => s.ScanDate)
                    .FirstOrDefaultAsync();

                return scan;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting latest security scan for code snippet ID {snippetId}");
                return null;
            }
        }

        // Check JavaScript/TypeScript security
        private List<SecurityFinding> CheckJavaScriptSecurity(string code)
        {
            var findings = new List<SecurityFinding>();

            // Check for eval usage
            var evalMatches = Regex.Matches(code, @"\beval\s*\(");

            foreach (Match match in evalMatches)
            {
                findings.Add(new SecurityFinding
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Use of eval()",
                    Severity = "High",
                    Description = "The eval() function executes arbitrary JavaScript code, which can lead to code injection vulnerabilities if the input is not properly sanitized.",
                    LineNumber = GetLineNumber(code, match.Index),
                    CodeSnippet = GetContextSnippet(code, match.Index),
                    Recommendation = "Avoid using eval(). Consider safer alternatives such as JSON.parse() for JSON data.",
                    Reference = "https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/eval#never_use_eval!"
                });
            }

            // Check for innerHTML
            var innerHTMLMatches = Regex.Matches(code, @"\.innerHTML\s*=");

            foreach (Match match in innerHTMLMatches)
            {
                findings.Add(new SecurityFinding
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Potential XSS via innerHTML",
                    Severity = "Medium",
                    Description = "Using innerHTML with untrusted data can lead to Cross-Site Scripting (XSS) vulnerabilities.",
                    LineNumber = GetLineNumber(code, match.Index),
                    CodeSnippet = GetContextSnippet(code, match.Index),
                    Recommendation = "Use textContent instead of innerHTML when dealing with untrusted data, or use a DOM API like document.createElement() to create elements safely.",
                    Reference = "https://owasp.org/www-community/attacks/xss/"
                });
            }

            // Check for document.write
            var documentWriteMatches = Regex.Matches(code, @"document\.write\s*\(");

            foreach (Match match in documentWriteMatches)
            {
                findings.Add(new SecurityFinding
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Use of document.write()",
                    Severity = "Medium",
                    Description = "Using document.write() can lead to XSS vulnerabilities if user input is not properly sanitized.",
                    LineNumber = GetLineNumber(code, match.Index),
                    CodeSnippet = GetContextSnippet(code, match.Index),
                    Recommendation = "Avoid using document.write() and use DOM manipulation methods instead.",
                    Reference = "https://developer.mozilla.org/en-US/docs/Web/API/Document/write#notes"
                });
            }

            return findings;
        }

        // Check Python security
        private List<SecurityFinding> CheckPythonSecurity(string code)
        {
            var findings = new List<SecurityFinding>();

            // Check for exec or eval usage
            var execMatches = Regex.Matches(code, @"\b(?:exec|eval)\s*\(");

            foreach (Match match in execMatches)
            {
                findings.Add(new SecurityFinding
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Use of exec() or eval()",
                    Severity = "High",
                    Description = "The exec() and eval() functions execute arbitrary Python code, which can lead to code injection vulnerabilities if the input is not properly sanitized.",
                    LineNumber = GetLineNumber(code, match.Index),
                    CodeSnippet = GetContextSnippet(code, match.Index),
                    Recommendation = "Avoid using exec() or eval(). Use safer alternatives such as ast.literal_eval() for evaluating expressions.",
                    Reference = "https://docs.python.org/3/library/functions.html#eval"
                });
            }

            // Check for shell=True in subprocess
            var shellTrueMatches = Regex.Matches(code, @"subprocess\.(?:call|run|Popen)\s*\([^)]*shell\s*=\s*True");

            foreach (Match match in shellTrueMatches)
            {
                findings.Add(new SecurityFinding
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Use of shell=True in subprocess",
                    Severity = "High",
                    Description = "Using shell=True with subprocess functions can lead to shell injection vulnerabilities if user input is included in the command.",
                    LineNumber = GetLineNumber(code, match.Index),
                    CodeSnippet = GetContextSnippet(code, match.Index),
                    Recommendation = "Avoid using shell=True and pass command arguments as a list instead.",
                    Reference = "https://docs.python.org/3/library/subprocess.html#security-considerations"
                });
            }

            // Check for SQL injection vulnerabilities
            var sqlMatches = Regex.Matches(code, @"(?:execute|executemany|cursor\.execute)\s*\([f""'].*?(?:\{|%s)");

            foreach (Match match in sqlMatches)
            {
                findings.Add(new SecurityFinding
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Potential SQL Injection",
                    Severity = "Critical",
                    Description = "String formatting or interpolation in SQL queries can lead to SQL injection vulnerabilities.",
                    LineNumber = GetLineNumber(code, match.Index),
                    CodeSnippet = GetContextSnippet(code, match.Index),
                    Recommendation = "Use parameterized queries with placeholders instead of string formatting or interpolation.",
                    Reference = "https://bobby-tables.com/python"
                });
            }

            return findings;
        }

        // Check C# security
        private List<SecurityFinding> CheckCSharpSecurity(string code)
        {
            var findings = new List<SecurityFinding>();

            // Check for dynamic SQL
            var sqlMatches = Regex.Matches(code, @"(?:ExecuteReader|ExecuteNonQuery|ExecuteScalar)\s*\(\s*[""'].*?\+");

            foreach (Match match in sqlMatches)
            {
                findings.Add(new SecurityFinding
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Potential SQL Injection",
                    Severity = "Critical",
                    Description = "String concatenation in SQL queries can lead to SQL injection vulnerabilities.",
                    LineNumber = GetLineNumber(code, match.Index),
                    CodeSnippet = GetContextSnippet(code, match.Index),
                    Recommendation = "Use parameterized queries with SqlParameters instead of string concatenation.",
                    Reference = "https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlcommand.parameters"
                });
            }

            // Check for insecure deserialization
            var binaryFormatterMatches = Regex.Matches(code, @"BinaryFormatter\.Deserialize");

            foreach (Match match in binaryFormatterMatches)
            {
                findings.Add(new SecurityFinding
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Insecure Deserialization",
                    Severity = "High",
                    Description = "BinaryFormatter.Deserialize is insecure as it can lead to remote code execution vulnerabilities.",
                    LineNumber = GetLineNumber(code, match.Index),
                    CodeSnippet = GetContextSnippet(code, match.Index),
                    Recommendation = "Use JSON or XML serialization with appropriate security settings instead.",
                    Reference = "https://docs.microsoft.com/en-us/dotnet/standard/serialization/binaryformatter-security-guide"
                });
            }

            // Check for directory traversal
            var pathMatches = Regex.Matches(code, @"(?:Path\.Combine|File\.(?:Open|Read|Write))\s*\([^)]*\+");

            foreach (Match match in pathMatches)
            {
                findings.Add(new SecurityFinding
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Potential Path Traversal",
                    Severity = "Medium",
                    Description = "String concatenation in file paths can lead to directory traversal vulnerabilities.",
                    LineNumber = GetLineNumber(code, match.Index),
                    CodeSnippet = GetContextSnippet(code, match.Index),
                    Recommendation = "Validate and sanitize user input for file paths. Consider using Path.GetFileName() to extract just the filename.",
                    Reference = "https://owasp.org/www-community/attacks/Path_Traversal"
                });
            }

            return findings;
        }

        // Check PHP security
        private List<SecurityFinding> CheckPHPSecurity(string code)
        {
            var findings = new List<SecurityFinding>();

            // Check for SQL injection
            var sqlMatches = Regex.Matches(code, @"mysqli_query\s*\(\s*\$[^,]+,\s*[""'][^""']*\$");

            foreach (Match match in sqlMatches)
            {
                findings.Add(new SecurityFinding
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Potential SQL Injection",
                    Severity = "Critical",
                    Description = "Direct inclusion of variables in SQL queries can lead to SQL injection vulnerabilities.",
                    LineNumber = GetLineNumber(code, match.Index),
                    CodeSnippet = GetContextSnippet(code, match.Index),
                    Recommendation = "Use prepared statements with mysqli_prepare() or PDO::prepare() instead.",
                    Reference = "https://www.php.net/manual/en/mysqli.prepare.php"
                });
            }

            // Check for reflected XSS
            var echoMatches = Regex.Matches(code, @"echo\s+\$_(?:GET|POST|REQUEST|COOKIE)\[");

            foreach (Match match in echoMatches)
            {
                findings.Add(new SecurityFinding
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Potential XSS Vulnerability",
                    Severity = "High",
                    Description = "Outputting user input directly can lead to Cross-Site Scripting (XSS) vulnerabilities.",
                    LineNumber = GetLineNumber(code, match.Index),
                    CodeSnippet = GetContextSnippet(code, match.Index),
                    Recommendation = "Use htmlspecialchars() or a template engine with proper escaping to output user input.",
                    Reference = "https://www.php.net/manual/en/function.htmlspecialchars.php"
                });
            }

            // Check for file inclusion
            var includeMatches = Regex.Matches(code, @"(?:include|require)(?:_once)?\s*\(\s*\$");

            foreach (Match match in includeMatches)
            {
                findings.Add(new SecurityFinding
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Potential File Inclusion Vulnerability",
                    Severity = "Critical",
                    Description = "Dynamic file inclusion can lead to Remote File Inclusion (RFI) or Local File Inclusion (LFI) vulnerabilities.",
                    LineNumber = GetLineNumber(code, match.Index),
                    CodeSnippet = GetContextSnippet(code, match.Index),
                    Recommendation = "Use a whitelist of allowed files and validate user input before including files.",
                    Reference = "https://owasp.org/www-project-web-security-testing-guide/latest/4-Web_Application_Security_Testing/07-Input_Validation_Testing/11.1-Testing_for_Local_File_Inclusion"
                });
            }

            return findings;
        }

        // Check Java security
        private List<SecurityFinding> CheckJavaSecurity(string code)
        {
            var findings = new List<SecurityFinding>();

            // Check for SQL injection
            var sqlMatches = Regex.Matches(code, @"(?:executeQuery|executeUpdate|execute)\s*\([""'][^""']*\+");

            foreach (Match match in sqlMatches)
            {
                findings.Add(new SecurityFinding
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Potential SQL Injection",
                    Severity = "Critical",
                    Description = "String concatenation in SQL queries can lead to SQL injection vulnerabilities.",
                    LineNumber = GetLineNumber(code, match.Index),
                    CodeSnippet = GetContextSnippet(code, match.Index),
                    Recommendation = "Use PreparedStatement with parameterized queries instead of string concatenation.",
                    Reference = "https://docs.oracle.com/javase/tutorial/jdbc/basics/prepared.html"
                });
            }

            // Check for dangerous deserialization
            var deserializeMatches = Regex.Matches(code, @"(?:ObjectInputStream|XMLDecoder).*?\.read");

            foreach (Match match in deserializeMatches)
            {
                findings.Add(new SecurityFinding
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Insecure Deserialization",
                    Severity = "High",
                    Description = "Java deserialization can lead to remote code execution vulnerabilities if not properly secured.",
                    LineNumber = GetLineNumber(code, match.Index),
                    CodeSnippet = GetContextSnippet(code, match.Index),
                    Recommendation = "Validate the source of serialized data and consider using safer serialization formats like JSON.",
                    Reference = "https://cheatsheetseries.owasp.org/cheatsheets/Deserialization_Cheat_Sheet.html"
                });
            }

            // Check for potential command injection
            var runtimeExecMatches = Regex.Matches(code, @"Runtime\.getRuntime\(\)\.exec\s*\([^)]*\+");

            foreach (Match match in runtimeExecMatches)
            {
                findings.Add(new SecurityFinding
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Potential Command Injection",
                    Severity = "Critical",
                    Description = "Including user input in exec() calls can lead to command injection vulnerabilities.",
                    LineNumber = GetLineNumber(code, match.Index),
                    CodeSnippet = GetContextSnippet(code, match.Index),
                    Recommendation = "Avoid using runtime exec with user input. If necessary, validate and sanitize the input carefully.",
                    Reference = "https://cheatsheetseries.owasp.org/cheatsheets/OS_Command_Injection_Defense_Cheat_Sheet.html"
                });
            }

            return findings;
        }

        // Check SQL security
        private List<SecurityFinding> CheckSQLSecurity(string code)
        {
            var findings = new List<SecurityFinding>();

            // Check for SELECT *
            var selectStarMatches = Regex.Matches(code, @"SELECT\s+\*\s+FROM", RegexOptions.IgnoreCase);

            foreach (Match match in selectStarMatches)
            {
                findings.Add(new SecurityFinding
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Use of SELECT *",
                    Severity = "Low",
                    Description = "Using SELECT * unnecessarily exposes all columns in a table and can lead to increased network traffic and application security risks.",
                    LineNumber = GetLineNumber(code, match.Index),
                    CodeSnippet = GetContextSnippet(code, match.Index),
                    Recommendation = "Specify only the columns you need instead of using *.",
                    Reference = "https://docs.microsoft.com/en-us/sql/t-sql/queries/select-transact-sql"
                });
            }

            // Check for GRANT ALL
            var grantAllMatches = Regex.Matches(code, @"GRANT\s+ALL", RegexOptions.IgnoreCase);

            foreach (Match match in grantAllMatches)
            {
                findings.Add(new SecurityFinding
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Overly Permissive Grants",
                    Severity = "High",
                    Description = "GRANT ALL provides excessive privileges, violating the principle of least privilege.",
                    LineNumber = GetLineNumber(code, match.Index),
                    CodeSnippet = GetContextSnippet(code, match.Index),
                    Recommendation = "Grant only the specific privileges required for each user or role.",
                    Reference = "https://cheatsheetseries.owasp.org/cheatsheets/Database_Security_Cheat_Sheet.html"
                });
            }

            return findings;
        }

        // Helper methods

        // Get line number from character index
        private int GetLineNumber(string code, int index)
        {
            if (string.IsNullOrEmpty(code) || index < 0 || index >= code.Length)
                return -1;

            int lineCount = 1;
            for (int i = 0; i < index && i < code.Length; i++)
            {
                if (code[i] == '\n')
                {
                    lineCount++;
                }
            }

            return lineCount;
        }

        // Get context snippet around an issue
        private string GetContextSnippet(string code, int index)
        {
            if (string.IsNullOrEmpty(code) || index < 0 || index >= code.Length)
                return string.Empty;

            // Find the start of the line
            int start = index;
            while (start > 0 && code[start - 1] != '\n')
            {
                start--;
            }

            // Find the end of the line
            int end = index;
            while (end < code.Length && code[end] != '\n')
            {
                end++;
            }

            // Extract line
            string line = code.Substring(start, end - start);

            // Add line number
            int lineNumber = GetLineNumber(code, index);

            return $"Line {lineNumber}: {line}";
        }
    }
}