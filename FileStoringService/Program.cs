using FileStoringService.Data;
using FileStoringService.Services;
using Microsoft.EntityFrameworkCore;

namespace FileStoringService;

public class Program
{
    public static void Main(string[] args)
    {
        var app = BuildApp(args);

        InitializeDatabase(app);

        ConfigureApp(app);
    }

    private static WebApplication BuildApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Добавляем базовые сервисы
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Настройка базы данных
        builder.Services.AddDbContext<AppDbContext>(opt => 
            opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Регистрация сервисов
        builder.Services.AddScoped<IFileStorageService, FileStorageService>();

        // Настройка CORS
        builder.Services.AddCors(opt => 
            opt.AddPolicy("AllowAll", policy => 
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader()));

        return builder.Build();
    }

    private static void InitializeDatabase(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            db.Database.EnsureCreated();
            logger.LogInformation("Database initialized");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database initialization failed");
        }
    }

    private static void ConfigureApp(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors("AllowAll");
        app.MapControllers();
        app.Run();
    }
}