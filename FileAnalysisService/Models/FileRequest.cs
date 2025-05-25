namespace FileAnalisysService.Models;

public class FileRequest
{
    public Guid FileId { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public long FileSize { get; set; }
}