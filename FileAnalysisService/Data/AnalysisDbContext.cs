using FileAnalisysService.Models;
using Microsoft.EntityFrameworkCore;

namespace FileAnalisysService.Data;

public class AnalysisDbContext : DbContext
{
    public AnalysisDbContext(DbContextOptions<AnalysisDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    public virtual DbSet<FileAnalysisResult> AnalysisResults { get; init; }
    public virtual DbSet<Match> SimilarityMatches { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Конфигурация FileAnalysisResult
        modelBuilder.Entity<FileAnalysisResult>(e =>
        {
            e.ToTable("AnalysisResults");
            e.HasKey(x => x.Id);

            e.Property(x => x.FileId).IsRequired();
            e.Property(x => x.FileName).IsRequired().HasMaxLength(255);
            e.Property(x => x.ParagraphCount).IsRequired();
            e.Property(x => x.WordCount).IsRequired();
            e.Property(x => x.CharacterCount).IsRequired();
            e.Property(x => x.AnalysisDate).IsRequired();
            e.Property(x => x.WordCloudPath).IsRequired(false);

            e.HasIndex(x => x.FileId).IsUnique();

            // Связи
            e.HasMany(x => x.SimilarityMatches)
                .WithOne(x => x.FileAnalysisResult)
                .HasForeignKey(x => x.FileAnalysisResultId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Конфигурация SimilarityMatch
        modelBuilder.Entity<Match>(e => 
        {
            e.ToTable("SimilarityMatches");
            e.HasKey(x => x.Id);

            e.Property(x => x.MatchedFileId).IsRequired();
            e.Property(x => x.MatchedFileName).IsRequired().HasMaxLength(255);
            e.Property(x => x.SimilarityPercentage).IsRequired();

            e.HasIndex(x => x.MatchedFileId);
        });
    }
}