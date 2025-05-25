using System.Net.Http.Json;
using FileAnalisysService.Models;
using FileAnalisysService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace FileAnalisysService.Controllers;

[ApiController]
[Route("api/text-analysis")]
public class TextAnalysisController : ControllerBase
{
    private readonly IFileAnalysisService _analysisService;
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<TextAnalysisController> _log;
    private readonly string _storageServiceUrl;

    public TextAnalysisController(
        IFileAnalysisService analysisService,
        IHttpClientFactory clientFactory,
        IConfiguration config,
        ILogger<TextAnalysisController> logger)
    {
        _analysisService = analysisService;
        _clientFactory = clientFactory;
        _log = logger;
        _storageServiceUrl = config["FileStorage:ServiceUrl"] ?? "http://localhost:7002";
    }

    [HttpPost("process/{fileId:guid}")]
    [ProducesResponseType(typeof(AnalysisResult), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> ProcessFile(Guid fileId)
    {
        try
        {
            _log.LogDebug("Starting analysis for file {FileId}", fileId);

            var client = _clientFactory.CreateClient();
            var metadataResponse = await client.GetAsync($"{_storageServiceUrl}/api/files/meta/{fileId}");

            if (!metadataResponse.IsSuccessStatusCode)
            {
                _log.LogWarning("File metadata not found for {FileId}", fileId);
                return NotFound();
            }

            var metadata = await metadataResponse.Content.ReadFromJsonAsync<FileRequest>();
            if (metadata == null)
            {
                _log.LogError("Invalid metadata format for {FileId}", fileId);
                return StatusCode(500);
            }

            var contentResponse = await client.GetAsync($"{_storageServiceUrl}/api/files/content/{fileId}");
            if (!contentResponse.IsSuccessStatusCode)
            {
                return NotFound();
            }

            var fileData = await contentResponse.Content.ReadAsByteArrayAsync();
            var result = await _analysisService.AnalyzeFileAsync(fileId, fileData, metadata.FileName);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Analysis error for file {FileId}", fileId);
            return StatusCode(500, "Processing error");
        }
    }

    [HttpGet("result/{analysisId:guid}")]
    [ProducesResponseType(typeof(AnalysisResult), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetResult(Guid analysisId)
    {
        try 
        {
            var analysisResult = await _analysisService.GetAnalysisResultAsync(analysisId);
            return analysisResult == null ? NotFound() : Ok(analysisResult);
        } 
        catch 
        {
            return StatusCode(500);
        }
    }

    [HttpGet("all-results")]
    [ProducesResponseType(typeof(IEnumerable<AnalysisResult>), 200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetAllResults()
    {
        try
        {
            return Ok(await _analysisService.GetAllAnalysisResultsAsync());
        }
        catch
        {
            return StatusCode(500);
        }
    }

    [HttpGet("visualize/{fileId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> CreateVisualization(Guid fileId)
    {
        try
        {
            _log.LogInformation("Creating visualization for {FileId}", fileId);
            
            var existing = await _analysisService.GetAnalysisResultAsyncByFileId(fileId);
            if (existing != null)
            {
                var image = await _analysisService.GetWordCloudImageAsync(existing.Id);
                if (image != null) 
                {
                    return File(image, "image/png");
                }
            }

            var client = _clientFactory.CreateClient();
            var metaResponse = await client.GetAsync($"{_storageServiceUrl}/api/files/meta/{fileId}");
            if (!metaResponse.IsSuccessStatusCode) return NotFound();

            var meta = await metaResponse.Content.ReadFromJsonAsync<FileRequest>();
            if (meta == null) return StatusCode(500);

            var content = await client.GetAsync($"{_storageServiceUrl}/api/files/content/{fileId}");
            if (!content.IsSuccessStatusCode) return NotFound();

            var fileBytes = await content.Content.ReadAsByteArrayAsync();
            var visualization = await _analysisService.GenerateWordCloudAsync(fileBytes, meta.FileName);

            return File(visualization.imageData, "image/png");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Visualization error for {FileId}", fileId);
            return StatusCode(500);
        }
    }

    [HttpDelete("remove/{analysisId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> RemoveAnalysis(Guid analysisId)
    {
        try
        {
            var success = await _analysisService.DeleteAnalysisAsync(analysisId);
            return success ? Ok() : NotFound();
        }
        catch
        {
            return StatusCode(500);
        }
    }
}