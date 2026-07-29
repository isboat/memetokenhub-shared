namespace MemeTokenHub.Shared.Auth;

public sealed record JwtOptions
{
    public const string SectionName = "Jwt";
    public required string SecretKey { get; init; }
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public int ExpirationMinutes { get; init; } = 60;
    public int ClockSkewMinutes { get; init; } = 2;
}
