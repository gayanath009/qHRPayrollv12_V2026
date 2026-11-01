using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Domain.Entities;
using WorkflowEngine.Infrastructure.Persistence;

namespace WorkflowEngine.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext, IServiceProvider serviceProvider)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Global Exception Intercepted: {ex.Message}");
            await HandleExceptionAsync(httpContext, ex, serviceProvider);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, IServiceProvider serviceProvider)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
            await dbContext.SystemLogs.AddAsync(new SystemLog
            {
                LogLevel = "Error",
                Message = exception.Message,
                Exception = exception.ToString(),
                Source = exception.Source,
                Timestamp = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }
        catch (Exception dbEx)
        {
            _logger.LogError(dbEx, "Failed logging exception to database.");
        }

        var response = new
        {
            StatusCode = context.Response.StatusCode,
            Message = "An internal server error occurred while processing your request.",
            Details = exception.Message
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}
