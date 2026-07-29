using NUnit.Framework;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using MemeTokenHub.Shared.Auth;
using MemeTokenHub.Shared.Constants;
using MemeTokenHub.Shared.Dtos;
using MemeTokenHub.Shared.Errors;
using MemeTokenHub.Shared.Exceptions;
using MemeTokenHub.Shared.Extensions;
using MemeTokenHub.Shared.Messaging;
using MemeTokenHub.Shared.Telemetry;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MemeTokenHub.Shared.UnitTests;

[TestFixture]
public sealed class JwtTokenServiceTests
{
    private static readonly JwtOptions Options = new()
    {
        SecretKey = "a-development-key-that-is-at-least-32-bytes-long",
        Issuer = "mth-tests",
        Audience = "mth-services",
        ExpirationMinutes = 15,
        ClockSkewMinutes = 0
    };

    [Test]
    public void GeneratedToken_RoundTripsIdentityRoleAndCapabilities()
    {
        var service = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(Options));
        var token = service.GenerateToken("user-42", UserRoles.Creator, [CapabilityNames.ProjectsWrite]);
        var principal = service.ValidateToken(token);

        Assert.Multiple(() =>
        {
            Assert.That(principal.FindFirstValue(ClaimTypes.NameIdentifier), Is.EqualTo("user-42"));
            Assert.That(principal.IsInRole(UserRoles.Creator), Is.True);
            Assert.That(principal.HasClaim(CapabilityNames.ClaimType, CapabilityNames.ProjectsWrite), Is.True);
        });
    }

    [Test]
    public void ValidateToken_RejectsTokenSignedWithAnotherKey()
    {
        var issuer = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(Options with { SecretKey = "another-development-key-at-least-32-bytes-long" }));
        var validator = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(Options));
        Assert.That(() => validator.ValidateToken(issuer.GenerateToken("user-1", UserRoles.Authenticated)), Throws.TypeOf<UnauthorizedException>());
    }

    [Test]
    public void GenerateToken_RejectsShortSigningKey()
    {
        var service = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(Options with { SecretKey = "too-short" }));
        Assert.That(() => service.GenerateToken("user-1", UserRoles.Authenticated), Throws.TypeOf<InvalidOperationException>());
    }
}

[TestFixture]
public sealed class MessagingTests
{
    [Test]
    public void EventEnvelope_SerializesStableWireContractAndRoundTrips()
    {
        var payload = new TokenPublishedEvent("token-1", "user-1", "Moon", "MOON");
        var envelope = EventEnvelope<TokenPublishedEvent>.Create(payload, "token-service", "token-1", "corr-1");
        var json = EventSerializer.Serialize(envelope);
        var restored = EventSerializer.Deserialize<TokenPublishedEvent>(json);

        using var document = JsonDocument.Parse(json);
        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("eventType").GetString(), Is.EqualTo("TokenPublished"));
            Assert.That(document.RootElement.GetProperty("schemaVersion").GetInt32(), Is.EqualTo(1));
            Assert.That(restored.Payload, Is.EqualTo(payload));
            Assert.That(json, Does.Not.Contain("email").IgnoreCase);
            Assert.That(json, Does.Not.Contain("wallet").IgnoreCase);
        });
    }

    [Test]
    public async Task OutboxStoreClaimsOldestPendingMessageAndMarksItPublished()
    {
        var store = new InMemoryOutboxStore();
        var first = new OutboxMessage(Guid.NewGuid(), "First", 1, "{}", DateTimeOffset.UtcNow.AddMinutes(-1), "corr");
        var second = new OutboxMessage(Guid.NewGuid(), "Second", 1, "{}", DateTimeOffset.UtcNow, "corr");
        await store.AddAsync(second);
        await store.AddAsync(first);

        var claimed = await store.ClaimPendingAsync(1);
        await store.MarkPublishedAsync(claimed[0].EventId, DateTimeOffset.UtcNow);
        var remaining = await store.ClaimPendingAsync(10);

        Assert.Multiple(() =>
        {
            Assert.That(claimed.Single().EventId, Is.EqualTo(first.EventId));
            Assert.That(remaining.Single().EventId, Is.EqualTo(second.EventId));
        });
    }

    [Test]
    public async Task ProcessedEventStore_AllowsAnEventOnlyOnce()
    {
        var store = new InMemoryProcessedEventStore();
        var id = Guid.NewGuid();
        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => store.TryBeginAsync(id)));
        Assert.That(results.Count(x => x), Is.EqualTo(1));
    }
}

[TestFixture]
public sealed class ContractTests
{
    [Test]
    public void PagedResponse_ValidatesBounds()
    {
        var response = PagedResponse<int>.Create([1, 2], 20, 0, 2);
        Assert.That(response.Items, Is.EqualTo(new[] { 1, 2 }));
        Assert.That(() => PagedResponse<int>.Create([], 0, 0, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [TestCase("person@example.com", true)]
    [TestCase("not-an-email", false)]
    [TestCase(null, false)]
    public void EmailValidation_IsSafe(string? value, bool expected) => Assert.That(value.IsValidEmail(), Is.EqualTo(expected));

    [Test]
    public void NotFoundException_HasStandardContract()
    {
        var exception = new NotFoundException("Token", "123");
        Assert.Multiple(() =>
        {
            Assert.That(exception.StatusCode, Is.EqualTo(404));
            Assert.That(exception.Code, Is.EqualTo("NOT_FOUND"));
        });
    }
}

[TestFixture]
public sealed class MiddlewareTests
{
    [Test]
    public async Task ProblemMiddleware_ReturnsSafeRfc7807Response()
    {
        var middleware = new ProblemDetailsMiddleware(_ => throw new InvalidOperationException("sensitive stack detail"),
            NullLogger<ProblemDetailsMiddleware>.Instance, new TestEnvironment());
        var context = new DefaultHttpContext { TraceIdentifier = "trace-123" };
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(context.Response.StatusCode, Is.EqualTo(500));
            Assert.That(context.Response.ContentType, Is.EqualTo("application/problem+json"));
            Assert.That(document.RootElement.GetProperty("detail").GetString(), Does.Not.Contain("sensitive"));
            Assert.That(document.RootElement.GetProperty("traceId").GetString(), Is.EqualTo("trace-123"));
        });
    }

    [Test]
    public async Task CorrelationMiddleware_PreservesValidIncomingId()
    {
        var middleware = new CorrelationMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationHeaders.CorrelationId] = "request-123";
        await middleware.InvokeAsync(context);
        await context.Response.StartAsync();
        Assert.That(context.Response.Headers[CorrelationHeaders.CorrelationId].ToString(), Is.EqualTo("request-123"));
    }

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = "/";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
