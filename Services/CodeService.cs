using CodeVault.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CodeVault.Data;
using Microsoft.Extensions.Logging;
using System;

namespace CodeVault.Services
{
    public class CodeService
    {
        private readonly CodeDbContext _context;
        private readonly VectorEmbeddingService _vectorService;
        private readonly ILogger<CodeService> _logger;

        public CodeService(
            CodeDbContext context,
            VectorEmbeddingService vectorService,
            ILogger<CodeService> logger)
        {
            _context = context;
            _vectorService = vectorService;
            _logger = logger;
        }

        // Get all code snippets
        public async Task<List<CodeSnippet>> GetAllAsync()
        {
            return await _context.CodeSnippets
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        // Get code snippet by id
        public async Task<CodeSnippet> GetByIdAsync(int id)
        {
            return await _context.CodeSnippets.FindAsync(id);
        }

        // Add new code snippet
        public async Task<CodeSnippet> AddAsync(string title, string content, string language, string description, string[] tags)
        {
            var codeSnippet = new CodeSnippet
            {
                Title = title,
                Content = content,
                Language = language,
                Description = description ?? string.Empty,
                Tags = tags ?? Array.Empty<string>(),
                CreatedAt = DateTime.UtcNow
            };

            _context.CodeSnippets.Add(codeSnippet);
            await _context.SaveChangesAsync();
            return codeSnippet;
        }

        // Add code snippet with embeddings generation
        public async Task<CodeSnippet> AddWithEmbeddingsAsync(string title, string content, string language, string description, string[] tags)
        {
            // First add the code snippet
            var codeSnippet = await AddAsync(title, content, language, description, tags);

            try
            {
                // Generate and store embeddings
                await _vectorService.StoreEmbeddingsForCodeSnippetAsync(codeSnippet);
                _logger.LogInformation($"Generated embeddings for new code snippet ID {codeSnippet.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to generate embeddings for code snippet ID {codeSnippet.Id}");
                // Don't throw - the code snippet was still created successfully
            }

            // Process tags
            await ProcessTagsAsync(codeSnippet.Tags);

            return codeSnippet;
        }

        // Update existing code snippet
        public async Task<CodeSnippet> UpdateAsync(int id, string title, string content, string language, string description, string[] tags)
        {
            var codeSnippet = await _context.CodeSnippets.FindAsync(id);

            if (codeSnippet == null)
                throw new KeyNotFoundException($"Code snippet with ID {id} not found.");

            codeSnippet.Title = title;
            codeSnippet.Content = content;
            codeSnippet.Language = language;
            codeSnippet.Description = description ?? string.Empty;
            codeSnippet.Tags = tags ?? Array.Empty<string>();
            codeSnippet.UpdatedAt = DateTime.UtcNow;

            _context.CodeSnippets.Update(codeSnippet);
            await _context.SaveChangesAsync();

            // Process tags
            await ProcessTagsAsync(codeSnippet.Tags);

            return codeSnippet;
        }

        // Update code snippet with embedding regeneration
        public async Task<CodeSnippet> UpdateWithEmbeddingsAsync(int id, string title, string content, string language, string description, string[] tags)
        {
            // First update the code snippet
            var codeSnippet = await UpdateAsync(id, title, content, language, description, tags);

            try
            {
                // Regenerate embeddings
                await _vectorService.StoreEmbeddingsForCodeSnippetAsync(codeSnippet);
                _logger.LogInformation($"Updated embeddings for code snippet ID {codeSnippet.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to update embeddings for code snippet ID {codeSnippet.Id}");
                // Don't throw - the code snippet was still updated successfully
            }

            return codeSnippet;
        }

        // Delete code snippet
        public async Task DeleteAsync(int id)
        {
            var codeSnippet = await _context.CodeSnippets.FindAsync(id);

            if (codeSnippet == null)
                throw new KeyNotFoundException($"Code snippet with ID {id} not found.");

            _context.CodeSnippets.Remove(codeSnippet);
            await _context.SaveChangesAsync();

            // Note: We don't need to explicitly delete the embeddings as they'll be
            // cascade deleted due to the FK relationship configured in the DbContext
        }

        // Basic search with text matching
        public async Task<List<CodeSnippet>> SearchAsync(string searchTerm, string language = null)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetFilteredSnippetsAsync(language);

            // Split search term into keywords for better matching
            var keywords = searchTerm.Split(' ', ',', '.', '?', '!')
                .Where(k => k.Length > 2) // Only consider words longer than 2 chars
                .ToList();

            if (!keywords.Any())
                return await GetFilteredSnippetsAsync(language);

            // Build query with multiple OR conditions
            var query = _context.CodeSnippets.AsQueryable();

            // Apply language filter if provided
            if (!string.IsNullOrEmpty(language))
            {
                query = query.Where(c => c.Language == language);
            }

            // Start with an initial filter
            query = query.Where(c =>
                c.Title.Contains(searchTerm) ||
                c.Content.Contains(searchTerm) ||
                c.Description.Contains(searchTerm) ||
                c.TagString.Contains(searchTerm));

            // For more specific keyword filtering
            foreach (var keyword in keywords)
            {
                var term = keyword;
                query = query.Where(c =>
                    c.Title.Contains(term) ||
                    c.Content.Contains(term) ||
                    c.Description.Contains(term) ||
                    c.TagString.Contains(term));
            }

            return await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        // Vector-based semantic search 
        public async Task<List<CodeSnippet>> SearchWithVectorAsync(string searchTerm, int limit = 5, string language = null)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetFilteredSnippetsAsync(language, limit);

            try
            {
                // Use vector similarity search for better semantic matching
                _logger.LogInformation($"Performing vector search for: {searchTerm}");
                var results = await _vectorService.SearchSimilarCodeSnippetsAsync(searchTerm, limit, language);
                _logger.LogInformation($"Vector search found {results.Count} results");
                return results;
            }
            catch (Exception ex)
            {
                // Log the error but don't bubble it up
                _logger.LogError(ex, $"Vector search failed for term: {searchTerm}. Falling back to text search.");

                // Fallback to basic text search if vector search fails
                return await SearchAsync(searchTerm, language);
            }
        }

        // Get snippets by language
        public async Task<List<CodeSnippet>> GetByLanguageAsync(string language, int limit = 50)
        {
            if (string.IsNullOrEmpty(language))
                return await GetAllAsync();

            return await _context.CodeSnippets
                .Where(c => c.Language == language)
                .OrderByDescending(c => c.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        // Get snippets by tag
        public async Task<List<CodeSnippet>> GetByTagAsync(string tag, int limit = 50)
        {
            if (string.IsNullOrEmpty(tag))
                return await GetAllAsync();

            return await _context.CodeSnippets
                .Where(c => c.TagString.Contains(tag))
                .OrderByDescending(c => c.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        // Increment view count
        public async Task IncrementViewCountAsync(int id)
        {
            var snippet = await _context.CodeSnippets.FindAsync(id);
            if (snippet != null)
            {
                snippet.ViewCount++;
                await _context.SaveChangesAsync();
            }
        }

        // Increment usage count
        public async Task IncrementUsageCountAsync(int id)
        {
            var snippet = await _context.CodeSnippets.FindAsync(id);
            if (snippet != null)
            {
                snippet.UsageCount++;
                await _context.SaveChangesAsync();
            }
        }

        // Add or update rating
        public async Task<double> AddRatingAsync(int id, int rating)
        {
            if (rating < 1 || rating > 5)
                throw new ArgumentException("Rating must be between 1 and 5");

            var snippet = await _context.CodeSnippets.FindAsync(id);
            if (snippet == null)
                throw new KeyNotFoundException($"Code snippet with ID {id} not found.");

            // Calculate new average rating
            var totalRating = snippet.Rating * snippet.RatingCount;
            snippet.RatingCount++;
            totalRating += rating;
            snippet.Rating = totalRating / snippet.RatingCount;

            await _context.SaveChangesAsync();
            return snippet.Rating;
        }

        // Get all tags with usage counts
        public async Task<List<CodeTag>> GetAllTagsAsync()
        {
            return await _context.CodeTags
                .OrderByDescending(t => t.UsageCount)
                .ToListAsync();
        }

        // Process tags for a code snippet
        private async Task ProcessTagsAsync(string[] tags)
        {
            if (tags == null || !tags.Any())
                return;

            foreach (var tagName in tags)
            {
                var normalizedTag = tagName.Trim().ToLower();
                if (string.IsNullOrWhiteSpace(normalizedTag))
                    continue;

                var existingTag = await _context.CodeTags
                    .FirstOrDefaultAsync(t => t.Name.ToLower() == normalizedTag);

                if (existingTag != null)
                {
                    // Increment usage count for existing tag
                    existingTag.UsageCount++;
                    _context.CodeTags.Update(existingTag);
                }
                else
                {
                    // Create new tag
                    var newTag = new CodeTag
                    {
                        Name = normalizedTag,
                        UsageCount = 1,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.CodeTags.Add(newTag);
                }
            }

            await _context.SaveChangesAsync();
        }

        // Helper method to get filtered snippets
        private async Task<List<CodeSnippet>> GetFilteredSnippetsAsync(string language = null, int limit = 50)
        {
            var query = _context.CodeSnippets.AsQueryable();

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