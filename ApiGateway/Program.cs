using Microsoft.AspNetCore.HttpLogging;

namespace APIGateway;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

// Конфигурация Reverse Proxy
        builder.Services
            .AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Базовое логирование HTTP-запросов
        builder.Services.AddHttpLogging(o => {
            o.LoggingFields = HttpLoggingFields.All;
        });

        var app = builder.Build();

// Pipeline обработки запросов
        app.UseHttpLogging();
        app.MapReverseProxy();

        app.Run();
    }
}