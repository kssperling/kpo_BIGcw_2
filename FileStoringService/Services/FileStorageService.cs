using FileStoringService.Data;
using FileStoringService.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FileStoringService.Services;

public class FileStorageService : IFileStorageService
{
    private readonly ILogger<FileStorageService> _logger;
    private readonly string _storageDirectory;
    private readonly AppDbContext _dbContext;

    public FileStorageService(
        ILogger<FileStorageService> logger,
        IConfiguration configuration,
        AppDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
        _storageDirectory = configuration["FileStorage:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "FileStorage");

        if (!Directory.Exists(_storageDirectory))
        {
            Directory.CreateDirectory(_storageDirectory);
        }
    }

    private async Task<string> CalculateFileHashAsync(IFormFile file)
    {
        using var md5 = MD5.Create();
        using var stream = file.OpenReadStream();
        var hashBytes = await md5.ComputeHashAsync(stream);
        return Convert.ToBase64String(hashBytes);
    }

    public async Task<FileMetadata> SaveFileAsync(IFormFile file)
    {
        try
        {
            string fileHash = await CalculateFileHashAsync(file);

            var existingFile = await _dbContext.Files.FirstOrDefaultAsync(f => f.FileHash == fileHash);
            if (existingFile != null)
            {
                _logger.LogInformation("Файл с таким хешем уже существует, возвращаем существующий ID: {FileId}", existingFile.Id);
                return existingFile;
            }

            var fileId = Guid.NewGuid();
            var fileName = Path.GetFileName(file.FileName);
            var filePath = Path.Combine(_storageDirectory, fileId.ToString());

            var metadata = new FileMetadata
            {
                Id = fileId,
                FileName = fileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                UploadDate = DateTime.UtcNow,
                FilePath = filePath,
                FileHash = fileHash
            };

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            await _dbContext.Files.AddAsync(metadata);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Файл {FileName} успешно сохранен с ID {FileId}", fileName, fileId);

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при сохранении файла {FileName}", file.FileName);
            throw;
        }
    }

    public async Task<(byte[], string)> GetFileAsync(Guid id)
    {
        var metadata = await _dbContext.Files.FindAsync(id);
        if (metadata == null)
        {
            _logger.LogWarning("Файл с ID {FileId} не найден", id);
            throw new FileNotFoundException($"Файл с ID {id} не найден");
        }

        try
        {
            var fileBytes = await File.ReadAllBytesAsync(metadata.FilePath);
            return (fileBytes, metadata.ContentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при чтении файла с ID {FileId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<FileMetadata>> GetAllFilesMetadataAsync()
    {
        return await _dbContext.Files.ToListAsync();
    }

    public async Task<bool> DeleteFileAsync(Guid id)
    {
        var metadata = await _dbContext.Files.FindAsync(id);
        if (metadata == null || !File.Exists(metadata.FilePath))
        {
            _logger.LogWarning("Файл с ID {FileId} не найден при попытке удаления", id);
            return false;
        }

        try
        {
            File.Delete(metadata.FilePath);

            _dbContext.Files.Remove(metadata);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Файл с ID {FileId} успешно удален", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении файла с ID {FileId}", id);
            return false;
        }
    }

    public async Task<FileMetadata> GetFileMetadataAsync(Guid id)
    {
        var metadata = await _dbContext.Files.FindAsync(id);
        if (metadata == null)
        {
            _logger.LogWarning("Метаданные для файла с ID {FileId} не найдены", id);
            throw new FileNotFoundException($"Метаданные для файла с ID {id} не найдены");
        }

        return metadata;
    }
}