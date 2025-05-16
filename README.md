CodeVault
<p align="center">
  <img src="docs/images/logo.png" alt="CodeVault Logo" width="200"/>
</p>

# CodeVault

<p align="center">
  <strong>AI-powered code knowledge management system</strong><br>
  Store, search, analyze, and improve your code with semantic search and AI assistance
</p>
<<<<<<< HEAD
<p align="center">
  <a href="#key-features">Features</a> �
  <a href="#benefits-for-developers">Benefits</a> �
  <a href="#screenshots">Screenshots</a> �
  <a href="#getting-started">Getting Started</a> �
  <a href="#usage-examples">Usage Examples</a> �
  <a href="#api-reference">API</a> �
  <a href="#contributing">Contributing</a> �
  <a href="#license">License</a>
</p>

Overview
CodeVault is a comprehensive platform for developers to store, organize, and analyze code snippets across various programming languages. Powered by AI, it offers intelligent code explanations, security scanning, performance analysis, and smart search capabilities, making it the ultimate developer's knowledge base.
Key Features

?? AI Code Assistant - Get intelligent explanations, optimizations, and guidance with the integrated AI chat
?? Code Repository - Store, categorize, and search code snippets across multiple programming languages
?? Semantic Search - Advanced vector-based search finds semantically similar code beyond simple text matching
?? Security Scanner - Detect potential security vulnerabilities with detailed recommendations
?? Code Analysis Tools - Analyze complexity, quality, and optimization opportunities
?? Code Comparison - Compare different code approaches side-by-side
?? Conversation History - Keep track of your AI chat interactions for future reference

Benefits for Developers
?? Accelerate Development

Save time by quickly finding and reusing code snippets
Learn faster with AI-powered explanations of complex code
Improve productivity by building a personal knowledge base of working code
Avoid reinventing the wheel by leveraging your existing code solutions

??? Write Better, Safer Code

Identify security issues before they reach production
Optimize performance with AI-powered suggestions
Improve code quality through complexity and structure analysis
Compare different approaches to understand tradeoffs

?? Seamless Integration

Modern web interface works across devices
REST API for integration with your development tools
Docker support for easy deployment
Open-source and extensible architecture

Screenshots
<div align="center">
  <p><strong>AI Chat Interface</strong></p>
  <img src="docs/images/chat-screenshot.png" alt="CodeVault AI Chat" width="800"/>
</div>
<div align="center">
  <p><strong>Code Snippet Repository</strong></p>
  <img src="docs/images/snippets-screenshot.png" alt="CodeVault Code Snippets" width="800"/>
</div>
Getting Started
Prerequisites

Docker and Docker Compose
.NET 8.0 SDK (for development)
PostgreSQL with pgvector extension
OpenAI API key or Ollama (for local AI models)

Quick Start with Docker

Clone the repository:

bashgit clone https://github.com/yourusername/codevault.git
cd codevault

Set your OpenAI API key:

bash# Create a .env file
echo "OPENAI_API_KEY=your_api_key_here" > .env

Start the application:

bashdocker-compose up -d

Access CodeVault at http://localhost:8080

Manual Setup

Clone the repository:

bashgit clone https://github.com/yourusername/codevault.git
cd codevault

Install PostgreSQL with pgvector extension:

bash# Ubuntu/Debian
=======

<p align="center">
  <a href="#key-features">Features</a> •
  <a href="#benefits-for-developers">Benefits</a> •
  <a href="#screenshots">Screenshots</a> •
  <a href="#getting-started">Getting Started</a> •
  <a href="#usage-examples">Usage Examples</a> •
  <a href="#api-reference">API</a> •
  <a href="#contributing">Contributing</a> •
  <a href="#license">License</a>
</p>

---

## Overview

CodeVault is a comprehensive platform for developers to store, organize, and analyze code snippets across various programming languages. Powered by AI, it offers intelligent code explanations, security scanning, performance analysis, and smart search capabilities, making it the ultimate developer's knowledge base.

## Key Features

- 🧠 **AI Code Assistant** - Get intelligent explanations, optimizations, and guidance with the integrated AI chat
- 📚 **Code Repository** - Store, categorize, and search code snippets across multiple programming languages
- 🔍 **Semantic Search** - Advanced vector-based search finds semantically similar code beyond simple text matching
- 🔒 **Security Scanner** - Detect potential security vulnerabilities with detailed recommendations
- 📊 **Code Analysis Tools** - Analyze complexity, quality, and optimization opportunities
- 🔄 **Code Comparison** - Compare different code approaches side-by-side
- 📝 **Conversation History** - Keep track of your AI chat interactions for future reference

## Benefits for Developers

### 🚀 Accelerate Development

- **Save time** by quickly finding and reusing code snippets
- **Learn faster** with AI-powered explanations of complex code
- **Improve productivity** by building a personal knowledge base of working code
- **Avoid reinventing the wheel** by leveraging your existing code solutions

### 🛡️ Write Better, Safer Code

- **Identify security issues** before they reach production
- **Optimize performance** with AI-powered suggestions
- **Improve code quality** through complexity and structure analysis
- **Compare different approaches** to understand tradeoffs

### 🔄 Seamless Integration

- **Modern web interface** works across devices
- **REST API** for integration with your development tools
- **Docker support** for easy deployment
- **Open-source** and extensible architecture

## Screenshots

<div align="center">
  <p><strong>AI Chat Interface</strong></p>
  <img src="docs/images/Screenshot 2025-05-16 104446.png" alt="CodeVault AI Chat" width="800"/>
</div>

<div align="center">
  <p><strong>Code Snippet Repository</strong></p>
  <img src="docs/images/Screenshot 2025-05-16 104801.png" alt="CodeVault Code Snippets Repository" width="800"/>
</div>

## Getting Started

### Prerequisites

- Docker and Docker Compose
- .NET 8.0 SDK (for development)
- PostgreSQL with pgvector extension
- OpenAI API key or Ollama (for local AI models)

### Quick Start with Docker

1. Clone the repository:
```bash
git clone https://github.com/George-Sakellariou/codevault.git
cd codevault
```

2. Set your OpenAI API key:
```bash
# Create a .env file
echo "OPENAI_API_KEY=your_api_key_here" > .env
```

3. Start the application:
```bash
docker-compose up -d
```

4. Access CodeVault at `http://localhost:8080`

### Manual Setup

1. Clone the repository:
```bash
git clone https://github.com/George-Sakellariou/codevault.git
cd codevault
```

2. Install PostgreSQL with pgvector extension:
```bash
# Ubuntu/Debian
>>>>>>> 7bdff4a4f9e6f9d8d997693a8605d7b56b6877c8
sudo apt update
sudo apt install postgresql postgresql-contrib
sudo apt install postgresql-server-dev-$(pg_config --version | awk '{print $2}' | cut -d. -f1)
git clone https://github.com/pgvector/pgvector.git
cd pgvector
make
sudo make install
cd ..
<<<<<<< HEAD

Create database and user:

bashsudo -u postgres psql
=======
```

3. Create database and user:
```bash
sudo -u postgres psql
>>>>>>> 7bdff4a4f9e6f9d8d997693a8605d7b56b6877c8
postgres=# CREATE USER codevault-user WITH PASSWORD 'securepass123!';
postgres=# CREATE DATABASE code_vault;
postgres=# GRANT ALL PRIVILEGES ON DATABASE code_vault TO codevault-user;
postgres=# \c code_vault
code_vault=# CREATE EXTENSION vector;
code_vault=# \q
<<<<<<< HEAD

Configure API keys:

bash# Edit appsettings.json or use user secrets
dotnet user-secrets init
dotnet user-secrets set "OpenAI:ApiKey" "your_api_key_here"

Run database migrations:

bashdotnet ef database update

Start the application:

bashdotnet run
Usage Examples
Adding a Code Snippet

Navigate to Code Snippets > Add New Snippet
Fill in the details:

Title: "Async/Await Error Handling Pattern"
Language: JavaScript
Content:

javascriptasync function fetchData() {
  try {
    const response = await fetch('https://api.example.com/data');
    if (!response.ok) {
      throw new Error(`HTTP error: ${response.status}`);
    }
    const data = await response.json();
    return data;
  } catch (error) {
    console.error('Fetch error:', error);
    throw error;  // Re-throw for caller handling
  }
}

// Usage with error handling
async function loadData() {
  try {
    const result = await fetchData();
    processData(result);
  } catch (error) {
    displayError(error);
  }
}

Description: "A robust pattern for handling errors in async/await functions, including proper error propagation and HTTP status checking."
Tags: async, error-handling, javascript, fetch-api


Click Save Code Snippet

Running a Security Scan

Find the code snippet you want to scan
Note its ID (visible in the URL or details page)
Navigate to Tools > Security Scanner
Enter the snippet ID and click Scan for Vulnerabilities
Review the results, which might include:

Hardcoded credentials
Insecure API calls
Potential XSS vulnerabilities
SQL injection risks



Example vulnerable code to test (NodeJS):
javascriptfunction authenticateUser(req, res) {
=======
```

4. Configure API keys:
```bash
# Edit appsettings.json or use user secrets
dotnet user-secrets init
dotnet user-secrets set "OpenAI:ApiKey" "your_api_key_here"
```

5. Run database migrations:
```bash
dotnet ef database update
```

6. Start the application:
```bash
dotnet run
```

## Usage Examples

### Adding a Code Snippet

1. Navigate to `Code Snippets` > `Add New Snippet`
2. Fill in the details:
   - Title: "Async/Await Error Handling Pattern"
   - Language: JavaScript
   - Content:
   ```javascript
   async function fetchData() {
     try {
       const response = await fetch('https://api.example.com/data');
       if (!response.ok) {
         throw new Error(`HTTP error: ${response.status}`);
       }
       const data = await response.json();
       return data;
     } catch (error) {
       console.error('Fetch error:', error);
       throw error;  // Re-throw for caller handling
     }
   }
   
   // Usage with error handling
   async function loadData() {
     try {
       const result = await fetchData();
       processData(result);
     } catch (error) {
       displayError(error);
     }
   }
   ```
   - Description: "A robust pattern for handling errors in async/await functions, including proper error propagation and HTTP status checking."
   - Tags: async, error-handling, javascript, fetch-api

3. Click `Save Code Snippet`

### Running a Security Scan

1. Find the code snippet you want to scan
2. Note its ID (visible in the URL or details page)
3. Navigate to `Tools` > `Security Scanner`
4. Enter the snippet ID and click `Scan for Vulnerabilities`
5. Review the results, which might include:
   - Hardcoded credentials
   - Insecure API calls
   - Potential XSS vulnerabilities
   - SQL injection risks

Example vulnerable code to test (NodeJS):
```javascript
function authenticateUser(req, res) {
>>>>>>> 7bdff4a4f9e6f9d8d997693a8605d7b56b6877c8
  const username = req.body.username;
  const password = req.body.password;
  
  // VULNERABLE: Hardcoded credentials
  if (username === "admin" && password === "admin123") {
    return res.json({ token: "secret-jwt-token" });
  }
  
  // VULNERABLE: SQL Injection
  const query = `SELECT * FROM users WHERE username = '${username}' AND password = '${password}'`;
  db.query(query, (err, result) => {
    if (err) throw err;
    if (result.length > 0) {
      // VULNERABLE: Insecure token generation
      const token = Math.random().toString(36).substring(2);
      res.json({ token: token });
    } else {
      res.status(401).json({ error: "Invalid credentials" });
    }
  });
}
<<<<<<< HEAD
Code Analysis for Optimization

Find or add a code snippet
Navigate to Tools > Code Analysis
Enter the snippet ID and click Analyze
Review complexity metrics and optimization suggestions

Example code to analyze (Python):
pythondef fibonacci(n):
=======
```

### Code Analysis for Optimization

1. Find or add a code snippet
2. Navigate to `Tools` > `Code Analysis`
3. Enter the snippet ID and click `Analyze`
4. Review complexity metrics and optimization suggestions

Example code to analyze (Python):
```python
def fibonacci(n):
>>>>>>> 7bdff4a4f9e6f9d8d997693a8605d7b56b6877c8
    if n <= 0:
        return 0
    elif n == 1:
        return 1
    else:
        return fibonacci(n-1) + fibonacci(n-2)

def calculate_fibonacci_sequence(length):
    result = []
    for i in range(length):
        result.append(fibonacci(i))
    return result

# Generate a sequence
sequence = calculate_fibonacci_sequence(30)
print(sequence)
<<<<<<< HEAD
The analysis will suggest replacing recursive Fibonacci with dynamic programming for better performance.
Asking the AI Assistant

Navigate to the Chat tab
Ask a code-related question:

"Can you explain how promises work in JavaScript?"
"What's the difference between ArrayList and LinkedList in Java?"
"How can I optimize this SQL query? SELECT * FROM orders WHERE customer_id IN (SELECT id FROM customers WHERE region = 'Europe')"
"What are the security risks in this code?" (paste code snippet)



The AI will provide an answer, often referencing relevant code from your repository.
Comparing Code Snippets

Navigate to Tools > Code Comparison
Enter the IDs of two code snippets to compare
Review the detailed comparison highlighting:

Structural differences
Complexity variations
Performance implications
Common patterns



Advanced Features
Vector-Based Semantic Search
CodeVault uses advanced embeddings to enable semantic search:
csharp// Example from CodeVault's search service
=======
```

The analysis will suggest replacing recursive Fibonacci with dynamic programming for better performance.

### Asking the AI Assistant

1. Navigate to the `Chat` tab
2. Ask a code-related question:
   - "Can you explain how promises work in JavaScript?"
   - "What's the difference between ArrayList and LinkedList in Java?"
   - "How can I optimize this SQL query? SELECT * FROM orders WHERE customer_id IN (SELECT id FROM customers WHERE region = 'Europe')"
   - "What are the security risks in this code?" (paste code snippet)

The AI will provide an answer, often referencing relevant code from your repository.

### Comparing Code Snippets

1. Navigate to `Tools` > `Code Comparison`
2. Enter the IDs of two code snippets to compare
3. Review the detailed comparison highlighting:
   - Structural differences
   - Complexity variations
   - Performance implications
   - Common patterns

## Advanced Features

### Vector-Based Semantic Search

CodeVault uses advanced embeddings to enable semantic search:

```csharp
// Example from CodeVault's search service
>>>>>>> 7bdff4a4f9e6f9d8d997693a8605d7b56b6877c8
public async Task<List<CodeSnippet>> SearchWithVectorAsync(string query, int limit = 5)
{
    // Generate embeddings for the search query
    var queryEmbeddings = await _vectorService.GetEmbeddingsAsync(query);
    
    // Perform cosine similarity search in the vector database
    // This finds semantically similar code, not just text matches
    var results = await _vectorDatabase.SearchSimilarAsync(queryEmbeddings, limit);
    
    return results;
}
<<<<<<< HEAD
This allows you to find code based on concepts, not just keywords.
Custom Performance Metrics
Track performance metrics for your code snippets:
csharp// Add time complexity metric
=======
```

This allows you to find code based on concepts, not just keywords.

### Custom Performance Metrics

Track performance metrics for your code snippets:

```csharp
// Add time complexity metric
>>>>>>> 7bdff4a4f9e6f9d8d997693a8605d7b56b6877c8
await _analysisService.AddPerformanceMetricAsync(
    snippetId: 42,
    metricName: "Time Complexity",
    metricValue: "O(n log n)",
    environment: "Node.js 18.x",
    notes: "Measured with array size of 10,000 elements"
);

// Add memory usage metric
await _analysisService.AddPerformanceMetricAsync(
    snippetId: 42,
    metricName: "Memory Usage",
    metricValue: "12.5",
    numericValue: 12.5,
    unit: "MB",
    environment: "Node.js 18.x"
);
<<<<<<< HEAD
API Reference
CodeVault provides a REST API for integration with your development tools.
Authentication
All API endpoints require authentication using API keys.
bash# Example API request with authentication
curl -X GET https://your-codevault-instance.com/api/code/search?query=async%20function \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer your_api_key_here"
Code Snippets API
Search Code Snippets
GET /api/code/search?query={query}&language={language}
Get Code Snippet
GET /api/code/{id}
Create Code Snippet
POST /api/code
Request body:
json{
=======
```

## API Reference

CodeVault provides a REST API for integration with your development tools.

### Authentication

All API endpoints require authentication using API keys.

```bash
# Example API request with authentication
curl -X GET https://your-codevault-instance.com/api/code/search?query=async%20function \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer your_api_key_here"
```

### Code Snippets API

#### Search Code Snippets

```
GET /api/code/search?query={query}&language={language}
```

#### Get Code Snippet

```
GET /api/code/{id}
```

#### Create Code Snippet

```
POST /api/code
```

Request body:
```json
{
>>>>>>> 7bdff4a4f9e6f9d8d997693a8605d7b56b6877c8
  "title": "Async Error Handling",
  "content": "async function fetchData() {...}",
  "language": "JavaScript",
  "description": "Error handling pattern for async functions",
  "tags": ["async", "error-handling", "javascript"]
}
<<<<<<< HEAD
Update Code Snippet
PUT /api/code/{id}
Delete Code Snippet
DELETE /api/code/{id}
Analysis API
Analyze Code Complexity
POST /api/analysis/complexity
Request body:
json{
  "snippetId": 42
}
Get Optimization Info
POST /api/analysis/optimization
Analyze Security
POST /api/analysis/security
Compare Snippets
POST /api/analysis/compare
Request body:
json{
  "snippetId1": 42,
  "snippetId2": 43
}
Add Performance Metric
POST /api/analysis/metrics
Request body:
json{
=======
```

#### Update Code Snippet

```
PUT /api/code/{id}
```

#### Delete Code Snippet

```
DELETE /api/code/{id}
```

### Analysis API

#### Analyze Code Complexity

```
POST /api/analysis/complexity
```

Request body:
```json
{
  "snippetId": 42
}
```

#### Get Optimization Info

```
POST /api/analysis/optimization
```

#### Analyze Security

```
POST /api/analysis/security
```

#### Compare Snippets

```
POST /api/analysis/compare
```

Request body:
```json
{
  "snippetId1": 42,
  "snippetId2": 43
}
```

#### Add Performance Metric

```
POST /api/analysis/metrics
```

Request body:
```json
{
>>>>>>> 7bdff4a4f9e6f9d8d997693a8605d7b56b6877c8
  "snippetId": 42,
  "metricName": "Time Complexity",
  "metricValue": "O(n)",
  "unit": "",
  "environment": "Node.js 18.x",
  "notes": "Measured with array of 10,000 elements"
}
<<<<<<< HEAD
Chat API
Send Message
POST /api/chat/message
Request body:
json{
  "content": "Explain how promises work in JavaScript",
  "conversationId": 0
}
Get Conversations
GET /api/chat/conversations
Delete Conversation
DELETE /api/chat/conversations/{id}
Extending CodeVault
Adding New Languages
CodeVault supports multiple programming languages out of the box, but you can extend it with new ones:

Add language definition in GetLanguageClass() methods
Add language-specific security checks in SecurityAnalysisService.cs
Add syntax highlighting support in the frontend

Custom AI Integration
CodeVault works with OpenAI by default, but you can integrate other models:

Implement the IOpenAiService interface for your AI provider
Configure the service in Program.cs

Example integration with a local Ollama model:
csharppublic class OllamaService : IOpenAiService
=======
```

### Chat API

#### Send Message

```
POST /api/chat/message
```

Request body:
```json
{
  "content": "Explain how promises work in JavaScript",
  "conversationId": 0
}
```

#### Get Conversations

```
GET /api/chat/conversations
```

#### Delete Conversation

```
DELETE /api/chat/conversations/{id}
```

## Extending CodeVault

### Adding New Languages

CodeVault supports multiple programming languages out of the box, but you can extend it with new ones:

1. Add language definition in `GetLanguageClass()` methods
2. Add language-specific security checks in `SecurityAnalysisService.cs`
3. Add syntax highlighting support in the frontend

### Custom AI Integration

CodeVault works with OpenAI by default, but you can integrate other models:

1. Implement the `IOpenAiService` interface for your AI provider
2. Configure the service in `Program.cs`

Example integration with a local Ollama model:

```csharp
public class OllamaService : IOpenAiService
>>>>>>> 7bdff4a4f9e6f9d8d997693a8605d7b56b6877c8
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _model;
    
    public OllamaService(IConfiguration configuration)
    {
        _httpClient = new HttpClient();
        _baseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434/api";
        _model = configuration["Ollama:CompletionModel"] ?? "llama3";
    }
    
    public async Task<string> GetCompletionAsync(string prompt)
    {
        var requestData = new
        {
            model = _model,
            prompt = prompt
        };
        
        var content = new StringContent(
            JsonSerializer.Serialize(requestData),
            Encoding.UTF8,
            "application/json");
            
        var response = await _httpClient.PostAsync($"{_baseUrl}/generate", content);
        response.EnsureSuccessStatusCode();
        
        var responseBody = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseBody);
        
        return responseObject.GetProperty("response").GetString() ?? "";
    }
}
<<<<<<< HEAD
Troubleshooting
Common Issues

Database Connection

Error: Host=localhost;Port=5432;Database=code_vault;Username=codevault-user;Password=securepass123!
Solution: Check that PostgreSQL is running and credentials are correct:
bash# Test connection
psql -h localhost -U codevault-user -d code_vault

Vector Extension

Error: ERROR: function vector_cosine_similarity(numeric[], numeric[]) does not exist
Solution: Verify pgvector is installed correctly:
bash# In PostgreSQL
\c code_vault
CREATE EXTENSION IF NOT EXISTS vector;
\dx

OpenAI API Key

Error: OpenAI API key is not configured
Solution: Check your API key is set in appsettings.json, environment variables, or user secrets.
Contributing
We welcome contributions to CodeVault! See CONTRIBUTING.md for details.
Development Setup

Fork and clone the repository
Install dependencies
Set up the development database
Run the application in development mode

bash# Run in development mode
dotnet watch run
License
This project is licensed under the MIT License - see the LICENSE.md file for details.

<p align="center">
  Made with ?? by the CodeVault Team
</p>
=======
```

## Troubleshooting

### Common Issues

1. **Database Connection**

Error: `Host=localhost;Port=5432;Database=code_vault;Username=codevault-user;Password=securepass123!`

Solution: Check that PostgreSQL is running and credentials are correct:
```bash
# Test connection
psql -h localhost -U codevault-user -d code_vault
```

2. **Vector Extension**

Error: `ERROR: function vector_cosine_similarity(numeric[], numeric[]) does not exist`

Solution: Verify pgvector is installed correctly:
```bash
# In PostgreSQL
\c code_vault
CREATE EXTENSION IF NOT EXISTS vector;
\dx
```

3. **OpenAI API Key**

Error: `OpenAI API key is not configured`

Solution: Check your API key is set in appsettings.json, environment variables, or user secrets.

### Development Setup

1. Fork and clone the repository
2. Install dependencies
3. Set up the development database
4. Run the application in development mode

```bash
# Run in development mode
dotnet watch run
```

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE.md](LICENSE.md) file for details.

---
>>>>>>> 7bdff4a4f9e6f9d8d997693a8605d7b56b6877c8
