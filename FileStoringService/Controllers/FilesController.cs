using FileStoringService.Models;
using FileStoringService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FileStoringService.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly IFileStorageService _storageService;
    private readonly ILogger<FilesController> _log;

    public FilesController(IFileStorageService storageService, ILogger<FilesController> logger)
    {
        _storageService = storageService;
        _log = logger;
    }

    [HttpPost("upload")]
    [ProducesResponseType(typeof(FileMetadata), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Необходимо предоставить файл");

        if (!file.ContentType.Equals("text/plain", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Только .txt файлы поддерживаются");

        try
        {
            var result = await _storageService.SaveFileAsync(file);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Upload error");
            return StatusCode(500, "Ошибка загрузки файла");
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Download(Guid id)
    {
        try
        {
            var (content, type) = await _storageService.GetFileAsync(id);
            return File(content, type);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Download error for {Id}", id);
            return StatusCode(500, "Ошибка получения файла");
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FileMetadata>), 200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            return Ok(await _storageService.GetAllFilesMetadataAsync());
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Get all files error");
            return StatusCode(500, "Ошибка получения списка файлов");
        }
    }

    [HttpGet("metadata/{id}")]
    [ProducesResponseType(typeof(FileMetadata), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetMetadata(Guid id)
    {
        try
        {
            return Ok(await _storageService.GetFileMetadataAsync(id));
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Metadata error for {Id}", id);
            return StatusCode(500, "Ошибка получения метаданных");
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Remove(Guid id)
    {
        try
        {
            var deleted = await _storageService.DeleteFileAsync(id);
            return deleted ? Ok() : NotFound();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Delete error for {Id}", id);
            return StatusCode(500, "Ошибка удаления файла");
        }
    }
}