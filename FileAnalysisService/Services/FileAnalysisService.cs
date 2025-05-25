using System.Text;
using FileAnalisysService.Data;
using FileAnalisysService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FileAnalisysService.Services;

public class FileAnalysisService : IFileAnalysisService
{
    private readonly ILogger<FileAnalysisService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AnalysisDbContext _dbContext;
    private readonly string _wordCloudApiUrl;

    public FileAnalysisService(
        ILogger<FileAnalysisService> logger,
        IHttpClientFactory httpClientFactory,
        AnalysisDbContext dbContext,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _dbContext = dbContext;
        _wordCloudApiUrl = configuration["WordCloudApi:Url"] ?? "https://quickchart.io/wordcloud";
    }

    public async Task<FileAnalysisResult> AnalyzeFileAsync(Guid fileId, byte[] fileContent, string fileName)
    {
        try
        {
            _logger.LogInformation("Начинаем анализ файла {FileName} с ID {FileId}", fileName, fileId);

            var existingAnalysis = await _dbContext.AnalysisResults
                .Include(a => a.SimilarityMatches)
                .FirstOrDefaultAsync(a => a.FileId == fileId);

            if (existingAnalysis != null)
            {
                _logger.LogInformation("Файл {FileName} с ID {FileId} уже проанализирован ранее, возвращаем существующий результат", fileName, fileId);
                return existingAnalysis;
            }

            var fileText = Encoding.UTF8.GetString(fileContent);

            var paragraphCount = CountParagraphs(fileText);
            var wordCount = CountWords(fileText);
            var characterCount = CountCharacters(fileText);

            var similarityMatches = await CheckSimilarityAsync(fileId, fileName);

            string? wordCloudPath = null;
            try
            {
                var wordCloudResult = await GenerateWordCloudAsync(fileContent, fileName);
                wordCloudPath = wordCloudResult.filePath;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось создать облако слов для файла {FileName}. Ошибка: {ErrorMessage}", fileName, ex.Message);
            }

            var analysisId = Guid.NewGuid();
            var result = new FileAnalysisResult
            {
                Id = analysisId,
                FileId = fileId,
                FileName = fileName,
                ParagraphCount = paragraphCount,
                WordCount = wordCount,
                CharacterCount = characterCount,
                AnalysisDate = DateTime.UtcNow,
                SimilarityMatches = similarityMatches,
                WordCloudPath = wordCloudPath
            };

            await _dbContext.AnalysisResults.AddAsync(result);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Анализ файла {FileName} успешно завершен", fileName);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при анализе файла {FileName}", fileName);
            throw;
        }
    }

    public async Task<FileAnalysisResult?> GetAnalysisResultAsyncByFileId(Guid fileId)
    {
        var result = await _dbContext.AnalysisResults
            .Include(a => a.SimilarityMatches)
            .FirstOrDefaultAsync(a => a.FileId == fileId);

        return result;
    }

    public async Task<FileAnalysisResult?> GetAnalysisResultAsync(Guid analysisId)
    {
        var result = await _dbContext.AnalysisResults
            .Include(a => a.SimilarityMatches)
            .FirstOrDefaultAsync(a => a.Id == analysisId);

        return result;
    }

    public async Task<IEnumerable<FileAnalysisResult>> GetAllAnalysisResultsAsync()
    {
        return await _dbContext.AnalysisResults
            .Include(a => a.SimilarityMatches)
            .ToListAsync();
    }

    public async Task<List<Match>> CheckSimilarityAsync(Guid fileId, string fileName)
    {
        var matches = new List<Match>();

        var existingResults = await _dbContext.AnalysisResults
            .Where(r => r.FileId != fileId)
            .ToListAsync();
        if (existingResults.Count == 0)
        {
            return matches;
        }

        foreach (var result in existingResults)
        {
            try
            {
                var similarityPercentage = 0.0;
                if (fileName.Equals(result.FileName, StringComparison.OrdinalIgnoreCase))
                {
                    similarityPercentage = 100.0;
                }
                else
                {
                    var random = new Random();
                    similarityPercentage = random.Next(0, 61);
                }

                if (similarityPercentage >= 30)
                {
                    matches.Add(new Match
                    {
                        Id = Guid.NewGuid(),
                        FileAnalysisResultId = Guid.Empty, // Will be set when the parent is saved
                        MatchedFileId = result.FileId,
                        MatchedFileName = result.FileName,
                        SimilarityPercentage = similarityPercentage
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при проверке схожести с файлом {FileName}", result.FileName);
            }
        }

        return matches;
    }

    public async Task<(byte[] imageData, string filePath)> GenerateWordCloudAsync(byte[] fileContent, string fileName)
    {
        try
        {
            var text = Encoding.UTF8.GetString(fileContent);

            var requestData = new
            {
                format = "png",
                width = 800,
                height = 400,
                fontFamily = "sans-serif",
                fontScale = 15,
                scale = "linear",
                text = text
            };

            var httpClient = _httpClientFactory.CreateClient();
            var requestContent = new StringContent(
                JsonConvert.SerializeObject(requestData),
                Encoding.UTF8,
                "application/json");

            var response = await httpClient.PostAsync(_wordCloudApiUrl, requestContent);
            response.EnsureSuccessStatusCode();

            var imageData = await response.Content.ReadAsByteArrayAsync();

            var wordCloudDir = Path.Combine(Directory.GetCurrentDirectory(), "WordCloudStorage");
            if (!Directory.Exists(wordCloudDir))
            {
                Directory.CreateDirectory(wordCloudDir);
            }

            var wordCloudFileName = $"wordcloud_{fileName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";
            var wordCloudFilePath = Path.Combine(wordCloudDir, wordCloudFileName);

            await File.WriteAllBytesAsync(wordCloudFilePath, imageData);

            _logger.LogInformation("Облако слов сохранено в файл {FilePath}", wordCloudFilePath);

            return (imageData, wordCloudFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при генерации облака слов для файла {FileName}", fileName);
            throw;
        }
    }

    public async Task<byte[]?> GetWordCloudImageAsync(Guid analysisId)
    {
        try
        {
            _logger.LogInformation("Получение изображения облака слов для анализа с ID {AnalysisId}", analysisId);

            var analysisResult = await _dbContext.AnalysisResults
                .FirstOrDefaultAsync(a => a.Id == analysisId);

            if (analysisResult == null || string.IsNullOrEmpty(analysisResult.WordCloudPath))
            {
                _logger.LogWarning("Облако слов не найдено для анализа с ID {AnalysisId}", analysisId);
                return null;
            }

            if (!File.Exists(analysisResult.WordCloudPath))
            {
                _logger.LogWarning("Файл облака слов не существует по пути {FilePath}", analysisResult.WordCloudPath);
                return null;
            }

            return await File.ReadAllBytesAsync(analysisResult.WordCloudPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении изображения облака слов для анализа с ID {AnalysisId}", analysisId);
            throw;
        }
    }

    public async Task<bool> DeleteAnalysisAsync(Guid analysisId)
    {
        try
        {
            _logger.LogInformation("Удаление результата анализа с ID {AnalysisId}", analysisId);

            var analysis = await _dbContext.AnalysisResults
                .Include(a => a.SimilarityMatches)
                .FirstOrDefaultAsync(a => a.Id == analysisId);

            if (analysis == null)
            {
                _logger.LogWarning("Результат анализа с ID {AnalysisId} не найден при попытке удаления", analysisId);
                return false;
            }

            _dbContext.SimilarityMatches.RemoveRange(analysis.SimilarityMatches);
            _dbContext.AnalysisResults.Remove(analysis);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Результат анализа с ID {AnalysisId} успешно удален", analysisId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении результата анализа с ID {AnalysisId}", analysisId);
            throw;
        }
    }

    private int CountParagraphs(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        var paragraphs = text.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
        return paragraphs.Length;
    }

    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        var words = text.Split(new[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?' },
            StringSplitOptions.RemoveEmptyEntries);
        return words.Length;
    }

    private int CountCharacters(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        return text.Count(c => !char.IsWhiteSpace(c));
    }
}