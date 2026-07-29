using System.Text;
using MemeTokenHub.Shared.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MemeTokenHub.Shared.Auth;

public static class AuthExtensions
{
    public static IServiceCollection AddMemeTokenHubAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.SectionName)).ValidateDataAnnotations().Validate(
            x => !string.IsNullOrWhiteSpace(x.Issuer) && !string.IsNullOrWhiteSpace(x.Audience) && Encoding.UTF8.GetByteCount(x.SecretKey ?? "") >= 32,
            "JWT issuer, audience, and a secret key of at least 32 bytes are required.").ValidateOnStart();
        var options = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("The Jwt configuration section is required.");
        if (Encoding.UTF8.GetByteCount(options.SecretKey) < 32) throw new OptionsValidationException(JwtOptions.SectionName, typeof(JwtOptions), ["Jwt:SecretKey must contain at least 32 bytes."]);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<TimeProvider, IOptions<JwtOptions>>((bearer, timeProvider, jwtOptions) =>
                bearer.TokenValidationParameters = JwtTokenService.CreateValidationParameters(jwtOptions.Value, timeProvider));
        services.AddAuthorization();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ITokenService, JwtTokenService>();
        return services;
    }

    public static AuthorizationPolicyBuilder RequireCapability(this AuthorizationPolicyBuilder policy, string capability) =>
        policy.RequireClaim(CapabilityNames.ClaimType, capability);
}
