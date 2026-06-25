using System.Text.Json;
using POS.Api.Contracts;

namespace POS.Api.Infrastructure.Logging;

public sealed class GlobalExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
        catch (Exception ex)
        {
            var traceId = context.TraceIdentifier;
            _logger.LogError("Unhandled API exception. TraceId: {TraceId}. ExceptionType: {ExceptionType}", traceId, ex.GetType().Name);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse(traceId, SafeErrorMessages.UnexpectedError);
            await JsonSerializer.SerializeAsync(context.Response.Body, response, JsonOptions);
        }
    }
}
