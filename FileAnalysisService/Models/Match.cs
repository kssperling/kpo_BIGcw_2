using System.Text.Json.Serialization;

namespace FileAnalisysService.Models;

public class Match
{
    public Guid Id { get; set; } // PK
    public Guid FileAnalysisResultId { get; set; } // FK
    public Guid MatchedFileId { get; set; } // Index
    public string? MatchedFileName { get; set; }
    public double SimilarityPercentage { get; set; }

    [JsonIgnore]
    public FileAnalysisResult? FileAnalysisResult { get; set; }
}