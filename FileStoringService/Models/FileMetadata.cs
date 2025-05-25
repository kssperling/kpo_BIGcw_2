namespace FileStoringService.Models;

public class FileMetadata
{
    public Guid Id { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public long FileSize { get; set; }
    public DateTime UploadDate { get; set; }
    public string FilePath { get; set; }
    public string FileHash { get; set; }
}