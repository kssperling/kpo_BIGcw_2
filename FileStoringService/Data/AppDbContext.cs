using FileStoringService.Models;
using Microsoft.EntityFrameworkCore;

namespace FileStoringService.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<FileMetadata> Files { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure the FileMetadata entity
        modelBuilder.Entity<FileMetadata>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired();
            entity.Property(e => e.ContentType).IsRequired();
            entity.Property(e => e.FileSize).IsRequired();
            entity.Property(e => e.UploadDate).IsRequired();
            entity.Property(e => e.FilePath).IsRequired();
            entity.Property(e => e.FileHash).IsRequired();

            // Create an index on FileHash for faster lookups
            entity.HasIndex(e => e.FileHash);
        });
    }
}