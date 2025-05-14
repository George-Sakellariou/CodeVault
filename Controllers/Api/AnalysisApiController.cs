using Microsoft.AspNetCore.Mvc;
using CodeVault.Models;
using CodeVault.Services;
using System.Threading.Tasks;
using System;

namespace CodeVault.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalysisApiController : ControllerBase
    {
        private readonly CodeService _codeService;
        private readonly CodeAnalysisService _analysisService;
        private readonly SecurityAnalysisService _securityService;
        private readonly CodeComparisonService _comparisonService;

        public AnalysisApiController(
            CodeService codeService,
            CodeAnalysisService analysisService,
            SecurityAnalysisService securityService,
            CodeComparisonService comparisonService)
        {
            _codeService = codeService;
            _analysisService = analysisService;
            _securityService = securityService;
            _comparisonService = comparisonService;
        }

        // POST: api/analysis/complexity
        [HttpPost("complexity")]
        public async Task<IActionResult> AnalyzeComplexity(AnalysisRequest request)
        {
            try
            {
                var snippet = await _codeService.GetByIdAsync(request.SnippetId);
                if (snippet == null)
                {
                    return NotFound();
                }

                var analysis = await _analysisService.AnalyzeComplexityAsync(snippet);
                return Ok(new { analysis });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/analysis/optimization
        [HttpPost("optimization")]
        public async Task<IActionResult> GetOptimizationInfo(AnalysisRequest request)
        {
            try
            {
                var snippet = await _codeService.GetByIdAsync(request.SnippetId);
                if (snippet == null)
                {
                    return NotFound();
                }

                var optimization = await _analysisService.GetOptimizationInfoAsync(snippet);
                return Ok(new { optimization });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/analysis/security
        [HttpPost("security")]
        public async Task<IActionResult> AnalyzeSecurity(AnalysisRequest request)
        {
            try
            {
                var snippet = await _codeService.GetByIdAsync(request.SnippetId);
                if (snippet == null)
                {
                    return NotFound();
                }

                var security = await _securityService.AnalyzeSecurityAsync(snippet);
                return Ok(new { security });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/analysis/compare
        [HttpPost("compare")]
        public async Task<IActionResult> CompareSnippets(ComparisonRequest request)
        {
            try
            {
                var snippet1 = await _codeService.GetByIdAsync(request.SnippetId1);
                var snippet2 = await _codeService.GetByIdAsync(request.SnippetId2);

                if (snippet1 == null || snippet2 == null)
                {
                    return NotFound();
                }

                var comparison = await _comparisonService.CompareSnippetsAsync(snippet1, snippet2);
                return Ok(new { comparison });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/analysis/metrics
        [HttpPost("metrics")]
        public async Task<IActionResult> AddPerformanceMetric(MetricRequest request)
        {
            try
            {
                var metric = await _analysisService.AddPerformanceMetricAsync(
                    request.SnippetId,
                    request.MetricName,
                    request.MetricValue,
                    request.NumericValue,
                    request.Unit,
                    request.Environment,
                    request.Notes);

                return Ok(metric);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/analysis/securityscan
        [HttpPost("securityscan")]
        public async Task<IActionResult> PerformSecurityScan(AnalysisRequest request)
        {
            try
            {
                var scan = await _securityService.PerformSecurityScanAsync(request.SnippetId);
                return Ok(scan);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class AnalysisRequest
    {
        public int SnippetId { get; set; }
    }

    public class ComparisonRequest
    {
        public int SnippetId1 { get; set; }
        public int SnippetId2 { get; set; }
    }

    public class MetricRequest
    {
        public int SnippetId { get; set; }
        public string MetricName { get; set; }
        public string MetricValue { get; set; }
        public double? NumericValue { get; set; }
        public string Unit { get; set; }
        public string Environment { get; set; }
        public string Notes { get; set; }
    }
}
