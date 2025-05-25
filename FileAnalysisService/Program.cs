using FileAnalisysService.Data;
using FileAnalisysService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FileAnalisysService;

public class Program
{
    public static void Main(string[] args)
    {
        var app = InitializeApp(args);

        SetupDatabase(app);
        
        app.UseCors("AllowAll");

        app.MapControllers();

        app.Run();
    }

    private static WebApplication InitializeApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Настройка контроллеров и JSON
        builder.Services.AddControllers().AddJsonOptions(opt => {
            opt.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            opt.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Настройка БД
        builder.Services.AddDbContext<AnalysisDbContext>(x => 
            x.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Настройка HttpClient
        builder.Services.AddHttpClient(Options.DefaultName, client => {})
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            });

        // Регистрация сервисов
        builder.Services.AddScoped<IFileAnalysisService, FileAnalysisService>();

        // Настройка CORS
        builder.Services.AddCors(options => {
            options.AddPolicy("AllowAll", policy => {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        return app;
    }

    private static void SetupDatabase(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AnalysisDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            dbContext.Database.EnsureCreated();
            logger.LogInformation("Database is ready");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database setup failed");
        }
    }
}