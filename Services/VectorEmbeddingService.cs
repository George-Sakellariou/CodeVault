using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using CodeVault.Models;
using CodeVault.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace CodeVault.Services
{
    public class VectorEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiUrl;
        private readonly CodeDbContext _dbContext;
        private readonly ILogger<VectorEmbeddingService> _logger;
        private readonly string _connectionString;
        private readonly string _embeddingModel;

        public VectorEmbeddingService(
            HttpClient httpClient,
            IConfiguration configuration,
            CodeDbContext dbContext,
            ILogger<VectorEmbeddingService> logger)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            _apiKey = configuration["OpenAI:ApiKey"] ??
                      Environment.GetEnvironmentVariable("OpenAI__ApiKey") ??
                      throw new InvalidOperationException("OpenAI API key not found.");

            // Use the Ollama embedding model if configured, otherwise default to OpenAI
            var useOllama = configuration.GetValue<bool>("Ollama:UseForEmbeddings");
            if (useOllama)
            {
                _apiUrl = configuration["Ollama:BaseUrl"] + "/embeddings";
                _embeddingModel = configuration["Ollama:EmbeddingModel"] ?? "nomic-embed-text";
                _logger.LogInformation($"Using Ollama for embeddings with model: {_embeddingModel}");
            }
            else
            {
                _apiUrl = "https://api.openai.com/v1/embeddings";
                _embeddingModel = "text-embedding-3-small";
                _logger.LogInformation($"Using OpenAI for embeddings with model: {_embeddingModel}");
            }
        }

        public async Task<float[]> GetEmbeddingsAsync(string text)
        {
            try
            {
                _logger.LogInformation($"Generating embeddings for text of length {text.Length}");

                bool isOllama = _apiUrl.Contains("ollama");
                object requestData;

                if (isOllama)
                {
                    requestData = new
                    {
                        model = _embeddingModel,
                        prompt = text
                    };
                }
                else
                {
                    requestData = new
                    {
                        model = _embeddingModel,
                        input = text
                    };
                }

                var content = new StringContent(
                    JsonSerializer.Serialize(requestData),
                    Encoding.UTF8,
                    "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                if (!isOllama)
                {
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
                }

                const int maxRetries = 3;
                int retryCount = 0;

                while (retryCount < maxRetries)
                {
                    try
                    {
                        var response = await _httpClient.PostAsync(_apiUrl, content);
                        response.EnsureSuccessStatusCode();

                        var responseBody = await response.Content.ReadAsStringAsync();
                        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseBody);

                        if (isOllama)
                        {
                            var embeddingData = responseObject.GetProperty("embedding");
                            var embeddings = new List<float>();

                            foreach (var value in embeddingData.EnumerateArray())
                            {
                                embeddings.Add(value.GetSingle());
                            }

                            return embeddings.ToArray();
                        }
                        else
                        {
                            var embeddingData = responseObject
                                .GetProperty("data")[0]
                                .GetProperty("embedding");

                            var embeddings = new List<float>();
                            foreach (var value in embeddingData.EnumerateArray())
                            {
                                embeddings.Add(value.GetSingle());
                            }

                            _logger.LogInformation($"Successfully generated embeddings with dimension: {embeddings.Count}");
                            return embeddings.ToArray();
                        }
                    }
                    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        retryCount++;
                        _logger.LogWarning($"Rate limited by API. Retry attempt {retryCount} of {maxRetries}");
                        await Task.Delay(2000 * retryCount);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in API call to get embeddings");
                        throw;
                    }
                }

                throw new Exception("Maximum retry count exceeded when calling embeddings API");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embeddings");
                throw;
            }
        }

        // Store embeddings for a code snippet
        public async Task<CodeEmbedding> StoreEmbeddingsForCodeSnippetAsync(CodeSnippet codeSnippet)
        {
            try
            {
                _logger.LogInformation($"Storing embeddings for code snippet ID {codeSnippet.Id}");

                // Check if embeddings already exist for this code snippet
                var existing = await _dbContext.CodeEmbeddings
                    .FirstOrDefaultAsync(ke => ke.CodeSnippetId == codeSnippet.Id);

                if (existing != null)
                {
                    _logger.LogInformation($"Updating embeddings for code snippet ID {codeSnippet.Id}");
                    _dbContext.CodeEmbeddings.Remove(existing);
                    await _dbContext.SaveChangesAsync();
                }

                var text = $"Title: {codeSnippet.Title}\nLanguage: {codeSnippet.Language}\n";

                if (!string.IsNullOrEmpty(codeSnippet.Description))
                {
                    text += $"Description: {codeSnippet.Description}\n";
                }

                text += $"\n{codeSnippet.Content}";

                if (!string.IsNullOrEmpty(codeSnippet.TagString))
                {
                    text += $"\nTags: {codeSnippet.TagString}";
                }

                const int maxChars = 8000;
                if (text.Length > maxChars)
                {
                    text = text.Substring(0, maxChars);
                    _logger.LogWarning($"Text truncated to {maxChars} characters for embedding generation");
                }
 
                var embeddingsArray = await GetEmbeddingsAsync(text);

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var cmd = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS vector", connection))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                    string embeddingString = "[" + string.Join(",", embeddingsArray) + "]";

                    using (var cmd = new NpgsqlCommand(@"
                        INSERT INTO ""CodeEmbeddings"" (""CodeSnippetId"", ""Embedding"", ""CreatedAt"")
                        VALUES (@codeSnippetId, @embedding::vector, @createdAt)
                        RETURNING ""Id""", connection))
                    {
                        cmd.Parameters.AddWithValue("codeSnippetId", codeSnippet.Id);
                        cmd.Parameters.AddWithValue("embedding", embeddingString);
                        cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);

                        var id = await cmd.ExecuteScalarAsync();

                        var embeddings = new CodeEmbedding
                        {
                            Id = Convert.ToInt32(id),
                            CodeSnippetId = codeSnippet.Id,
                            Embedding = embeddingsArray,
                            CreatedAt = DateTime.UtcNow
                        };

                        return embeddings;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error storing embeddings for code snippet ID {codeSnippet.Id}");
                throw;
            }
        }

        public async Task<List<CodeSnippet>> SearchSimilarCodeSnippetsAsync(string query, int limit = 5, string language = null)
        {
            try
            {
                _logger.LogInformation($"Performing vector search for: {query}");

                if (string.IsNullOrWhiteSpace(query))
                {
                    return await GetFilteredSnippetsAsync(language, limit);
                }

                var queryEmbeddings = await GetEmbeddingsAsync(query);
                var results = new List<CodeSnippet>();

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var cmd = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS vector", connection))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }

                    string embeddingString = "[" + string.Join(",", queryEmbeddings) + "]";

                    string sqlQuery;
                    if (!string.IsNullOrEmpty(language))
                    {
                        sqlQuery = @"
                        SELECT c.""Id"", c.""Title"", c.""Content"", c.""Language"", c.""Description"", 
                               c.""TagString"", c.""CreatedAt"", c.""UpdatedAt"", c.""ViewCount"", 
                               c.""UsageCount"", c.""Rating"", c.""RatingCount"",
                               1 - (ce.""Embedding""::vector <=> @queryVector::vector) as similarity
                        FROM ""CodeSnippets"" c
                        JOIN ""CodeEmbeddings"" ce ON c.""Id"" = ce.""CodeSnippetId""
                        WHERE c.""Language"" = @language
                        ORDER BY similarity DESC
                        LIMIT @limit";
                    }
                    else
                    {
                        sqlQuery = @"
                        SELECT c.""Id"", c.""Title"", c.""Content"", c.""Language"", c.""Description"", 
                               c.""TagString"", c.""CreatedAt"", c.""UpdatedAt"", c.""ViewCount"", 
                               c.""UsageCount"", c.""Rating"", c.""RatingCount"",
                               1 - (ce.""Embedding""::vector <=> @queryVector::vector) as similarity
                        FROM ""CodeSnippets"" c
                        JOIN ""CodeEmbeddings"" ce ON c.""Id"" = ce.""CodeSnippetId""
                        ORDER BY similarity DESC
                        LIMIT @limit";
                    }

                    using (var cmd = new NpgsqlCommand(sqlQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("queryVector", embeddingString);
                        cmd.Parameters.AddWithValue("limit", limit);

                        if (!string.IsNullOrEmpty(language))
                        {
                            cmd.Parameters.AddWithValue("language", language);
                        }

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new CodeSnippet
                                {
                                    Id = reader.GetInt32(0),
                                    Title = reader.GetString(1),
                                    Content = reader.GetString(2),
                                    Language = reader.GetString(3),
                                    Description = !reader.IsDBNull(4) ? reader.GetString(4) : string.Empty,
                                    TagString = !reader.IsDBNull(5) ? reader.GetString(5) : string.Empty,
                                    CreatedAt = reader.GetDateTime(6),
                                    UpdatedAt = !reader.IsDBNull(7) ? reader.GetDateTime(7) : (DateTime?)null,
                                    ViewCount = reader.GetInt32(8),
                                    UsageCount = reader.GetInt32(9),
                                    Rating = reader.GetDouble(10),
                                    RatingCount = reader.GetInt32(11)
                                });
                            }
                        }
                    }
                }

                _logger.LogInformation($"Vector search found {results.Count} results");
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in vector search for query: {Query}", query);

                _logger.LogInformation("Falling back to basic search");
                return await FallbackSearchAsync(query, limit, language);
            }
        }


        private async Task<List<CodeSnippet>> FallbackSearchAsync(string query, int limit, string language = null)
        {
            try
            {
                var dbQuery = _dbContext.CodeSnippets.AsQueryable();

                if (!string.IsNullOrEmpty(language))
                {
                    dbQuery = dbQuery.Where(c => c.Language == language);
                }

                return await dbQuery
                    .Where(c => c.Title.Contains(query) ||
                                c.Content.Contains(query) ||
                                c.Description.Contains(query) ||
                                c.TagString.Contains(query))
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in fallback search for query: {Query}", query);
                return new List<CodeSnippet>();
            }
        }

        private async Task<List<CodeSnippet>> GetFilteredSnippetsAsync(string language = null, int limit = 50)
        {
            var query = _dbContext.CodeSnippets.AsQueryable();

            if (!string.IsNullOrEmpty(language))
            {
                query = query.Where(c => c.Language == language);
            }

            return await query
                .OrderByDescending(c => c.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }
    }
}