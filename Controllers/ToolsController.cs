using Microsoft.AspNetCore.Mvc;
using CodeVault.Models;
using CodeVault.Services;
using System.Threading.Tasks;
using System;

namespace CodeVault.Controllers
{
    public class ToolsController : Controller
    {
        private readonly CodeService _codeService;
        private readonly CodeAnalysisService _analysisService;
        private readonly CodeComparisonService _comparisonService;
        private readonly SecurityAnalysisService _securityService;

        public ToolsController(
            CodeService codeService,
            CodeAnalysisService analysisService,
            CodeComparisonService comparisonService,
            SecurityAnalysisService securityService)
        {
            _codeService = codeService;
            _analysisService = analysisService;
            _comparisonService = comparisonService;
            _securityService = securityService;
        }

        // GET: Tools/Compare
        public IActionResult Compare()
        {
            return View();
        }

        // POST: Tools/Compare
        [HttpPost]
        public async Task<IActionResult> Compare(int snippetId1, int snippetId2)
        {
            try
            {
                var snippet1 = await _codeService.GetByIdAsync(snippetId1);
                var snippet2 = await _codeService.GetByIdAsync(snippetId2);

                if (snippet1 == null || snippet2 == null)
                {
                    return NotFound();
                }

                var comparison = await _comparisonService.CompareSnippetsAsync(snippet1, snippet2);
                ViewBag.ComparisonResult = comparison;

                return View(new Tuple<CodeSnippet, CodeSnippet>(snippet1, snippet2));
            }
            catch (Exception)
            {
                return RedirectToAction(nameof(Compare));
            }
        }

        // GET: Tools/Analyze
        public IActionResult Analyze()
        {
            return View();
        }

        // POST: Tools/Analyze
        [HttpPost]
        public async Task<IActionResult> Analyze(int snippetId)
        {
            try
            {
                var snippet = await _codeService.GetByIdAsync(snippetId);
                if (snippet == null)
                {
                    return NotFound();
                }

                var analysis = await _analysisService.AnalyzeComplexityAsync(snippet);
                var optimizations = await _analysisService.GetOptimizationInfoAsync(snippet);

                ViewBag.ComplexityAnalysis = analysis;
                ViewBag.OptimizationInfo = optimizations;

                return View(snippet);
            }
            catch (Exception)
            {
                return RedirectToAction(nameof(Analyze));
            }
        }

        // GET: Tools/Security
        public IActionResult Security()
        {
            return View();
        }

        // POST: Tools/Security
        [HttpPost]
        public async Task<IActionResult> Security(int snippetId)
        {
            try
            {
                var snippet = await _codeService.GetByIdAsync(snippetId);
                if (snippet == null)
                {
                    return NotFound();
                }

                var scan = await _securityService.PerformSecurityScanAsync(snippetId);
                ViewBag.SecurityScan = scan;

                return View(snippet);
            }
            catch (Exception)
            {
                return RedirectToAction(nameof(Security));
            }
        }

        // GET: Tools/Languages
        public IActionResult Languages()
        {
            return View();
        }
    }
}