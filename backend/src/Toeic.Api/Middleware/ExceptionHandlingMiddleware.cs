using System.Text.Json;
using Toeic.Application.Common.Exceptions;

namespace Toeic.Api.Middleware;

/// <summary>
/// Bắt AppException (và subclass) ném từ Application/Infrastructure, map sang HTTP status +
/// ProblemDetails đúng nghĩa. Exception hệ thống không mong muốn bị log đầy đủ nhưng chỉ trả về
/// thông báo chung (500) cho client — không rò rỉ chi tiết stack trace/nội bộ.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (AppException ex)
        {
            _logger.LogWarning(ex, "Handled application exception: {Message}", ex.Message);
            await WriteProblemAsync(context, ex.StatusCode, ex.Message, (ex as ValidationAppException)?.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Path}", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError,
                "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.", null);
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context, int statusCode, string title, IDictionary<string, string[]>? errors)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var problem = new
        {
            title,
            status = statusCode,
            traceId = context.TraceIdentifier,
            errors,
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        }));
    }
}
