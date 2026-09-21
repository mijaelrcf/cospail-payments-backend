using Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Api.Middleware;

/// <summary>
/// Middleware global para el manejo centralizado de excepciones.
/// </summary>
public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error");
            await WriteValidationProblemDetailsAsync(context, ex);
        }
        catch (ArgumentException ex)
        {
            await HandleKnownErrorAsync(
                context,
                ex,
                StatusCodes.Status400BadRequest,
                "Validation Error",
                ex.Message,
                "Validation error"
            );
        }
        catch (KeyNotFoundException ex)
        {
            await HandleKnownErrorAsync(
                context,
                ex,
                StatusCodes.Status404NotFound,
                "Not Found",
                ex.Message,
                "Resource not found"
            );
        }
        catch (UnauthorizedAccessException ex)
        {
            await HandleKnownErrorAsync(
                context,
                ex,
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                ex.Message,
                "Unauthorized"
            );
        }
        catch (BankOperationException ex)
        {
            // Rechazo funcional del banco (ej. mantenimiento): el mensaje ya
            // viene en español y apto para el usuario, se propaga en el detalle
            // con 502 en lugar de ocultarlo como 500 genérico. El detalle
            // completo queda en el log para diagnóstico.
            await HandleKnownErrorAsync(
                context,
                ex,
                StatusCodes.Status502BadGateway,
                "Error del banco",
                ex.Message,
                "Rechazo funcional de Banco Económico"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteProblemDetailsAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred."
            );
        }
    }

    private async Task HandleKnownErrorAsync(
        HttpContext context,
        Exception exception,
        int statusCode,
        string title,
        string detail,
        string logMessage
    )
    {
        _logger.LogWarning(exception, "{Message}", logMessage);
        await WriteProblemDetailsAsync(context, statusCode, title, detail);
    }

    private static async Task WriteValidationProblemDetailsAsync(
        HttpContext context,
        ValidationException ex
    )
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";

        var problem = new ValidationProblemDetails(
            ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
        )
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Error",
            Instance = context.Request.Path
        };

        await context.Response.WriteAsJsonAsync(problem);
    }

    private static async Task WriteProblemDetailsAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail
    )
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        await context.Response.WriteAsJsonAsync(problem);
    }
}
