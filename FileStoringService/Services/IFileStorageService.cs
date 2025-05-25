using FileStoringService.Models;
using Microsoft.AspNetCore.Http;

namespace FileStoringService.Services;

public interface IFileStorageService
{
    Task<FileMetadata> SaveFileAsync(IFormFile file);
    Task<(byte[], string)> GetFileAsync(Guid id);
    Task<IEnumerable<FileMetadata>> GetAllFilesMetadataAsync();
    Task<bool> DeleteFileAsync(Guid id);
    Task<FileMetadata> GetFileMetadataAsync(Guid id);
}