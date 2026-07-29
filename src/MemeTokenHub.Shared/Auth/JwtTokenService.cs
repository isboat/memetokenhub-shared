using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MemeTokenHub.Shared.Constants;
using MemeTokenHub.Shared.Exceptions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MemeTokenHub.Shared.Auth;

public interface ITokenService
{
    string GenerateToken(string userId, string role, IEnumerable<string>? capabilities = null, int? expirationMinutes = null);
    ClaimsPrincipal ValidateToken(string token);
}

public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider? timeProvider = null) : ITokenService
{
    private readonly JwtOptions _options = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public string GenerateToken(string userId, string role, IEnumerable<string>? capabilities = null, int? expirationMinutes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        var now = _timeProvider.GetUtcNow();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange((capabilities ?? []).Distinct(StringComparer.Ordinal).Select(x => new Claim(CapabilityNames.ClaimType, x)));
        var credentials = new SigningCredentials(GetKey(), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_options.Issuer, _options.Audience, claims, now.UtcDateTime,
            now.AddMinutes(expirationMinutes ?? _options.ExpirationMinutes).UtcDateTime, credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal ValidateToken(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        try
        {
            return new JwtSecurityTokenHandler().ValidateToken(token, CreateValidationParameters(_options, _timeProvider), out _);
        }
        catch (SecurityTokenException exception)
        {
            throw new UnauthorizedException("Invalid token").WithInnerException(exception);
        }
        catch (ArgumentException exception)
        {
            throw new UnauthorizedException("Invalid token").WithInnerException(exception);
        }
    }

    internal static TokenValidationParameters CreateValidationParameters(JwtOptions options, TimeProvider? timeProvider = null)
    {
        var clock = timeProvider ?? TimeProvider.System;
        return new()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromMinutes(options.ClockSkewMinutes),
            LifetimeValidator = (notBefore, expires, _, parameters) =>
            {
                if (expires is null) return false;
                var now = clock.GetUtcNow().UtcDateTime;
                return (notBefore is null || notBefore <= now + parameters.ClockSkew)
                    && expires >= now - parameters.ClockSkew;
            },
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };
    }

    private SymmetricSecurityKey GetKey()
    {
        if (Encoding.UTF8.GetByteCount(_options.SecretKey) < 32)
            throw new InvalidOperationException("Jwt:SecretKey must contain at least 32 bytes.");
        return new(Encoding.UTF8.GetBytes(_options.SecretKey));
    }
}

internal static class ExceptionExtensions
{
    public static T WithInnerException<T>(this T exception, Exception inner) where T : Exception
    {
        exception.Data["InnerExceptionType"] = inner.GetType().Name;
        return exception;
    }
}
