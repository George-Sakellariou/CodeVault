using Microsoft.AspNetCore.Mvc;
using CodeVault.Models;
using CodeVault.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace CodeVault.Controllers
{
    public class CodeViewController : Controller
    {
        private readonly CodeService _codeService;
        private readonly CodeAnalysisService _analysisService;
        private readonly SecurityAnalysisService _securityService;

        public CodeViewController(
            CodeService codeService,
            CodeAnalysisService analysisService,
            SecurityAnalysisService securityService)
        {
            _codeService = codeService;
            _analysisService = analysisService;
            _securityService = securityService;
        }

        // GET: CodeView
        public async Task<IActionResult> Index(string language = null, string tag = null, string searchTerm = null, string sortBy = "newest")
        {
            List<CodeSnippet> snippets;

            if (!string.IsNullOrEmpty(searchTerm))
            {
                snippets = await _codeService.SearchAsync(searchTerm, language);
            }
            else if (!string.IsNullOrEmpty(tag))
            {
                snippets = await _codeService.GetByTagAsync(tag);
            }
            else if (!string.IsNullOrEmpty(language))
            {
                snippets = await _codeService.GetByLanguageAsync(language);
            }
            else
            {
                snippets = await _codeService.GetAllAsync();
            }

            // Apply sorting
            snippets = ApplySorting(snippets, sortBy);

            return View(snippets);
        }

        // GET: CodeView/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var snippet = await _codeService.GetByIdAsync(id);
            if (snippet == null)
            {
                return NotFound();
            }

            // Increment view count
            await _codeService.IncrementViewCountAsync(id);

            // Get related snippets
            var relatedSnippets = await _codeService.SearchWithVectorAsync(snippet.Title, 3);
            relatedSnippets = relatedSnippets.Where(s => s.Id != snippet.Id).ToList();
            ViewBag.RelatedSnippets = relatedSnippets;

            // Get performance metrics
            var metrics = await _analysisService.GetMetricsAsync(id);
            ViewBag.PerformanceMetrics = metrics;

            // Get security scan info
            var securityScans = await _securityService.GetLatestScanAsync(id);
            ViewBag.SecurityScan = securityScans;

            return View(snippet);
        }

        // GET: CodeView/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CodeView/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CodeSnippet model, string[] tags)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _codeService.AddWithEmbeddingsAsync(
                        model.Title,
                        model.Content,
                        model.Language,
                        model.Description,
                        tags);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                }
            }
            return View(model);
        }

        // GET: CodeView/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var snippet = await _codeService.GetByIdAsync(id);
            if (snippet == null)
            {
                return NotFound();
            }
            return View(snippet);
        }

        // POST: CodeView/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CodeSnippet model, string[] tags)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _codeService.UpdateWithEmbeddingsAsync(
                        id,
                        model.Title,
                        model.Content,
                        model.Language,
                        model.Description,
                        tags);
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                }
            }
            return View(model);
        }

        // POST: CodeView/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _codeService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // POST: CodeView/AddRating
        [HttpPost]
        public async Task<IActionResult> AddRating(int id, int rating)
        {
            try
            {
                var newRating = await _codeService.AddRatingAsync(id, rating);
                return Json(new { success = true, rating = newRating });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Helper method to apply sorting
        private List<CodeSnippet> ApplySorting(List<CodeSnippet> snippets, string sortBy)
        {
            return sortBy switch
            {
                "oldest" => snippets.OrderBy(s => s.CreatedAt).ToList(),
                "popular" => snippets.OrderByDescending(s => s.ViewCount).ToList(),
                "rating" => snippets.OrderByDescending(s => s.Rating).ToList(),
                _ => snippets.OrderByDescending(s => s.CreatedAt).ToList(), // Default: newest
            };
        }
    }
}