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
        private readonly string _apiUrl = "https://api.openai.com/v1/embeddings";
        private readonly CodeDbContext _dbContext;
        private readonly ILogger<VectorEmbeddingService> _logger;
        private readonly string _connectionString;

        public VectorEmbeddingService(
            HttpClient httpClient,
            IConfiguration configuration,
            CodeDbContext dbContext,
            ILogger<VectorEmbeddingService> logger)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection");

            _apiKey = configuration["OpenAI:ApiKey"] ??
                      Environment.GetEnvironmentVariable("OpenAI__ApiKey");

            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException(
                    "OpenAI API key is not configured. Please set the OpenAI:ApiKey in your configuration " +
                    "or OpenAI__ApiKey environment variable.");
            }
        }

        // Get embeddings from OpenAI
        public async Task<float[]> GetEmbeddingsAsync(string text)
        {
            try
            {
                var requestData = new
                {
                    model = "text-embedding-ada-002",
                    input = text
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestData),
                    Encoding.UTF8,
                    "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                var response = await _httpClient.PostAsync(_apiUrl, content);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseBody);

                var embeddingData = responseObject
                    .GetProperty("data")[0]
                    .GetProperty("embedding");

                var embeddings = new List<float>();
                foreach (var value in embeddingData.EnumerateArray())
                {
                    embeddings.Add(value.GetSingle());
                }

                return embeddings.ToArray();
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
                // Check if embeddings already exist for this code snippet
                var existing = await _dbContext.CodeEmbeddings
                    .FirstOrDefaultAsync(ke => ke.CodeSnippetId == codeSnippet.Id);

                if (existing != null)
                {
                    _logger.LogInformation($"Updating embeddings for code snippet ID {codeSnippet.Id}");
                    // Delete the existing embeddings if the code snippet has been updated
                    _dbContext.CodeEmbeddings.Remove(existing);
                    await _dbContext.SaveChangesAsync();
                }

                // Combine title, language, description and content for better context
                var text = $"Title: {codeSnippet.Title}\nLanguage: {codeSnippet.Language}\nDescription: {codeSnippet.Description}\n\n{codeSnippet.Content}";
                if (!string.IsNullOrEmpty(codeSnippet.TagString))
                {
                    text += $"\nTags: {codeSnippet.TagString}";
                }

                // Get embeddings from OpenAI
                var embeddingsArray = await GetEmbeddingsAsync(text);

                // We need to store embeddings differently since we don't have a direct Vector type
                // We'll use raw SQL to insert the embeddings properly
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Enable the vector extension
                    using (var cmd = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS vector", connection))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // Insert the embeddings using a raw SQL query
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

        // Search for similar code snippets based on a query
        public async Task<List<CodeSnippet>> SearchSimilarCodeSnippetsAsync(string query, int limit = 5, string language = null)
        {
            try
            {
                // Get embeddings for the query
                var queryEmbeddings = await GetEmbeddingsAsync(query);

                // Since we can't use EF Core directly for vector operations,
                // we need to use raw SQL queries
                var results = new List<CodeSnippet>();

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Enable the vector extension
                    using (var cmd = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS vector", connection))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // Perform a cosine similarity search using a parameterized query
                    // We'll convert the embedding array to a string representation
                    string embeddingString = "[" + string.Join(",", queryEmbeddings) + "]";

                    // Build the SQL query based on whether a language filter is provided
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

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching similar code snippets");
                // Fallback to basic search if vector search fails
                return await FallbackSearchAsync(query, limit, language);
            }
        }

        // Fallback search method using basic text matching
        private async Task<List<CodeSnippet>> FallbackSearchAsync(string query, int limit, string language)
        {
            try
            {
                var dbQuery = _dbContext.CodeSnippets.AsQueryable();

                // Apply language filter if provided
                if (!string.IsNullOrEmpty(language))
                {
                    dbQuery = dbQuery.Where(c => c.Language == language);
                }

                // Check for basic text matches
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
                _logger.LogError(ex, "Error in fallback search");
                return new List<CodeSnippet>();
            }
        }
    }
}