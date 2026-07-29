using System.Diagnostics;
using System.Text.Json;
using MemeTokenHub.Shared.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MemeTokenHub.Shared.Errors;

public sealed class ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> logger, IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context).ConfigureAwait(false); }
        catch (Exception exception)
        {
            logger.LogError(exception, "Request failed with trace ID {TraceId}", Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier);
            await WriteProblemAsync(context, exception, environment.IsDevelopment()).ConfigureAwait(false);
        }
    }

    internal static async Task WriteProblemAsync(HttpContext context, Exception exception, bool includeDetail)
    {
        var known = exception as MemeTokenHubException;
        var status = known?.StatusCode ?? StatusCodes.Status500InternalServerError;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = status == 500 ? "An unexpected error occurred." : known!.Code,
            Detail = status == 500 && !includeDetail ? "The server could not complete the request." : exception.Message,
            Instance = context.Request.Path,
            Type = $"https://httpstatuses.com/{status}"
        };
        problem.Extensions["traceId"] = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        if (exception is ValidationException validation) problem.Extensions["errors"] = validation.Errors;
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(context.Response.Body, problem, new JsonSerializerOptions(JsonSerializerDefaults.Web), context.RequestAborted).ConfigureAwait(false);
    }
}
