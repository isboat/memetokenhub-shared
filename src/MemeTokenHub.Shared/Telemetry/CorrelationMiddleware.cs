using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace MemeTokenHub.Shared.Telemetry;

public static class CorrelationHeaders
{
    public const string CorrelationId = "X-Correlation-ID";
}

public interface ICorrelationContext
{
    string CorrelationId { get; }
}

public sealed class CorrelationContext(IHttpContextAccessor accessor) : ICorrelationContext
{
    public string CorrelationId => accessor.HttpContext?.Items[CorrelationHeaders.CorrelationId]?.ToString()
        ?? Activity.Current?.TraceId.ToString() ?? string.Empty;
}

public sealed class CorrelationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var supplied = context.Request.Headers[CorrelationHeaders.CorrelationId].FirstOrDefault();
        var correlationId = IsValid(supplied) ? supplied! : Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        context.Items[CorrelationHeaders.CorrelationId] = correlationId;
        context.Response.Headers[CorrelationHeaders.CorrelationId] = new StringValues(correlationId);
        await next(context).ConfigureAwait(false);
    }

    private static bool IsValid(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 128 && value.All(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.');
}
