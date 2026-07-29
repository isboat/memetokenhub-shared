using MemeTokenHub.Shared.Errors;
using MemeTokenHub.Shared.Logging;
using MemeTokenHub.Shared.Telemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MemeTokenHub.Shared.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddMemeTokenHubShared(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICorrelationContext, CorrelationContext>();
        services.AddScoped(typeof(IAppLogger<>), typeof(AppLogger<>));
        return services;
    }

    public static IApplicationBuilder UseMemeTokenHubShared(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationMiddleware>().UseMiddleware<ProblemDetailsMiddleware>();
}
