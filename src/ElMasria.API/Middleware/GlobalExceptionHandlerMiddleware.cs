using System.Net;
using System.Text.Json;
using ElMasria.Application.Common;
using ElMasria.Domain.Exceptions;

namespace ElMasria.API.Middleware;

/// <summary>
/// Global exception handler middleware. Catches all unhandled exceptions
/// and returns a consistent ApiResponse with Arabic error messages.
/// </summary>
public sealed class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    /// <summary>
    /// Initializes the middleware.
    /// </summary>
    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Processes the HTTP request, catching any unhandled exceptions.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, messageEn, errors) = exception switch
        {
            NotFoundException ex => (
                (int)HttpStatusCode.NotFound,
                ex.Message,
                ex.MessageEn ?? "Resource not found.",
                new List<ApiError>()),

            UnauthorizedException ex => (
                (int)HttpStatusCode.Forbidden,
                ex.Message,
                ex.MessageEn ?? "Access denied.",
                new List<ApiError>()),

            BusinessRuleException ex => (
                (int)HttpStatusCode.UnprocessableEntity,
                ex.Message,
                ex.MessageEn ?? "Business rule violation.",
                new List<ApiError>()),

            InsufficientStockException ex => (
                (int)HttpStatusCode.Conflict,
                ex.Message,
                ex.MessageEn ?? "Insufficient stock.",
                new List<ApiError>()),

            CouponExpiredException ex => (
                (int)HttpStatusCode.BadRequest,
                ex.Message,
                ex.MessageEn ?? "Coupon has expired.",
                new List<ApiError>()),

            CouponAlreadyUsedException ex => (
                (int)HttpStatusCode.Conflict,
                ex.Message,
                ex.MessageEn ?? "Coupon has already been used.",
                new List<ApiError>()),

            DomainException ex => (
                (int)HttpStatusCode.BadRequest,
                ex.Message,
                ex.MessageEn ?? "Domain validation error.",
                new List<ApiError>()),

            FluentValidation.ValidationException ex => (
                (int)HttpStatusCode.BadRequest,
                "بيانات غير صالحة",
                "Validation failed.",
                ex.Errors.Select(e => new ApiError
                {
                    Field = e.PropertyName,
                    Message = e.ErrorMessage
                }).ToList()),

            _ => (
                (int)HttpStatusCode.InternalServerError,
                "حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى لاحقاً.",
                "An unexpected error occurred.",
                new List<ApiError>())
        };

        // Log server errors with full detail (English for server logs)
        if (statusCode >= 500)
        {
            _logger.LogError(exception,
                "Unhandled exception: {Message} | Path: {Path} | Method: {Method}",
                exception.Message,
                context.Request.Path,
                context.Request.Method);
        }
        else
        {
            _logger.LogWarning(
                "Handled exception: {ExceptionType} | Message: {Message} | Path: {Path}",
                exception.GetType().Name,
                exception.Message,
                context.Request.Path);
        }

        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.StatusCode = statusCode;

        var response = ApiResponse<object>.Fail(statusCode, message, messageEn, errors);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, jsonOptions));
    }
}
