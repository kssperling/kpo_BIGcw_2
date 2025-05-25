using FileAnalisysService.Models;

namespace FileAnalisysService.Services;

public interface IFileAnalysisService
{
    Task<FileAnalysisResult> AnalyzeFileAsync(Guid fileId, byte[] fileContent, string fileName);
    Task<FileAnalysisResult?> GetAnalysisResultAsyncByFileId(Guid fileId);
    Task<FileAnalysisResult?> GetAnalysisResultAsync(Guid analysisId);
    Task<IEnumerable<FileAnalysisResult>> GetAllAnalysisResultsAsync();
    Task<List<Match>> CheckSimilarityAsync(Guid fileId, string fileName);
    Task<(byte[] imageData, string filePath)> GenerateWordCloudAsync(byte[] fileContent, string fileName);
    Task<byte[]?> GetWordCloudImageAsync(Guid analysisId);
    Task<bool> DeleteAnalysisAsync(Guid analysisId);
}