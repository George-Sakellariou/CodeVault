using Microsoft.AspNetCore.Mvc;
using CodeVault.Models;
using CodeVault.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace CodeVault.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class CodeApiController : ControllerBase
    {
        private readonly CodeService _codeService;
        private readonly CodeAnalysisService _analysisService;
        private readonly SecurityAnalysisService _securityService;

        public CodeApiController(
            CodeService codeService,
            CodeAnalysisService analysisService,
            SecurityAnalysisService securityService)
        {
            _codeService = codeService;
            _analysisService = analysisService;
            _securityService = securityService;
        }

        // GET: api/code/search?query={query}&language={language}
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<CodeSnippet>>> Search(string query, string language = null)
        {
            try
            {
                var results = await _codeService.SearchWithVectorAsync(query, 10, language);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/code/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<CodeSnippet>> GetById(int id)
        {
            var snippet = await _codeService.GetByIdAsync(id);
            if (snippet == null)
            {
                return NotFound();
            }

            // Increment view count
            await _codeService.IncrementViewCountAsync(id);

            return Ok(snippet);
        }

        // POST: api/code
        [HttpPost]
        public async Task<ActionResult<CodeSnippet>> Create(CodeSnippetRequest request)
        {
            try
            {
                var snippet = await _codeService.AddWithEmbeddingsAsync(
                    request.Title,
                    request.Content,
                    request.Language,
                    request.Description,
                    request.Tags);

                return CreatedAtAction(nameof(GetById), new { id = snippet.Id }, snippet);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // PUT: api/code/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, CodeSnippetRequest request)
        {
            try
            {
                await _codeService.UpdateWithEmbeddingsAsync(
                    id,
                    request.Title,
                    request.Content,
                    request.Language,
                    request.Description,
                    request.Tags);

                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // DELETE: api/code/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _codeService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/code/{id}/rating
        [HttpPost("{id}/rating")]
        public async Task<IActionResult> AddRating(int id, RatingRequest request)
        {
            try
            {
                var newRating = await _codeService.AddRatingAsync(id, request.Rating);
                return Ok(new { rating = newRating });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/code/tags
        [HttpGet("tags")]
        public async Task<ActionResult<IEnumerable<CodeTag>>> GetAllTags()
        {
            try
            {
                var tags = await _codeService.GetAllTagsAsync();
                return Ok(tags);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class CodeSnippetRequest
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string Language { get; set; }
        public string Description { get; set; }
        public string[] Tags { get; set; }
    }

    public class RatingRequest
    {
        public int Rating { get; set; }
    }
}