namespace FileAnalisysService.Models;

public class FileAnalysisResult
{
    public Guid Id { get; set; } // PK
    public Guid FileId { get; set; } // Index
    public string? FileName { get; set; }
    public int ParagraphCount { get; set; }
    public int WordCount { get; set; }
    public int CharacterCount { get; set; }
    public DateTime AnalysisDate { get; set; }
    public List<Match> SimilarityMatches { get; set; } = new List<Match>();
    public string? WordCloudPath { get; set; }
}