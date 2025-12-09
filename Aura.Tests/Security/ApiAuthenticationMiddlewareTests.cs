using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Aura.Api.Middleware;
using Aura.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace Aura.Tests.Security;

public class ApiAuthenticationMiddlewareTests
{
    // Test-only signing key - NOT for production use
    // This key is intentionally marked as test-only and should never be used in production
    private const string TestSigningKey = "UNIT-TEST-ONLY-jwt-signing-key-32chars";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    private readonly Mock<ILogger<ApiAuthenticationMiddleware>> _loggerMock;
    private readonly Mock<RequestDelegate> _nextMock;

    public ApiAuthenticationMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<ApiAuthenticationMiddleware>>();
        _nextMock = new Mock<RequestDelegate>();
    }

    private static ApiAuthenticationOptions CreateOptions(
        bool enableJwt = true,
        bool requireAuth = true,
        string? signingKey = TestSigningKey,
        string? issuer = TestIssuer,
        string? audience = TestAudience,
        bool validateLifetime = true)
    {
        return new ApiAuthenticationOptions
        {
            EnableJwtAuthentication = enableJwt,
            EnableApiKeyAuthentication = false,
            RequireAuthentication = requireAuth,
            JwtSecretKey = signingKey,
            JwtIssuer = issuer ?? string.Empty,
            JwtAudience = audience ?? string.Empty,
            ValidateLifetime = validateLifetime,
            ClockSkew = TimeSpan.FromMinutes(5),
            AnonymousEndpoints = new[] { "/health", "/healthz" }
        };
    }

    private static string GenerateValidToken(
        string signingKey = TestSigningKey,
        string issuer = TestIssuer,
        string audience = TestAudience,
        DateTime? expires = null,
        string subject = "test-user")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, subject),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires ?? DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private ApiAuthenticationMiddleware CreateMiddleware(ApiAuthenticationOptions options)
    {
        var optionsMock = new Mock<IOptions<ApiAuthenticationOptions>>();
        optionsMock.Setup(x => x.Value).Returns(options);

        return new ApiAuthenticationMiddleware(
            _nextMock.Object,
            _loggerMock.Object,
            optionsMock.Object);
    }

    [Fact]
    public async Task InvokeAsync_ValidJwtToken_CallsNext()
    {
        // Arrange
        var options = CreateOptions();
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Request.Headers.Authorization = $"Bearer {GenerateValidToken()}";

        var nextCalled = false;
        _nextMock.Setup(n => n(It.IsAny<HttpContext>()))
            .Callback(() => nextCalled = true)
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ValidJwtToken_SetsUserPrincipal()
    {
        // Arrange
        var options = CreateOptions();
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Request.Headers.Authorization = $"Bearer {GenerateValidToken(subject: "my-user-id")}";

        _nextMock.Setup(n => n(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.NotNull(context.User);
        var subClaim = context.User.FindFirst("sub");
        Assert.NotNull(subClaim);
        Assert.Equal("my-user-id", subClaim.Value);
    }

    [Fact]
    public async Task InvokeAsync_ExpiredToken_Returns401()
    {
        // Arrange
        var options = CreateOptions();
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";

        // Create token that expired 1 hour ago
        var expiredToken = GenerateValidToken(expires: DateTime.UtcNow.AddHours(-1));
        context.Request.Headers.Authorization = $"Bearer {expiredToken}";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_InvalidSignature_Returns401()
    {
        // Arrange
        var options = CreateOptions();
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";

        // Generate token with different signing key
        var invalidToken = GenerateValidToken(signingKey: "different-signing-key-minimum-32-chars");
        context.Request.Headers.Authorization = $"Bearer {invalidToken}";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WrongIssuer_Returns401()
    {
        // Arrange
        var options = CreateOptions();
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";

        // Generate token with different issuer
        var invalidToken = GenerateValidToken(issuer: "WrongIssuer");
        context.Request.Headers.Authorization = $"Bearer {invalidToken}";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WrongAudience_Returns401()
    {
        // Arrange
        var options = CreateOptions();
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";

        // Generate token with different audience
        var invalidToken = GenerateValidToken(audience: "WrongAudience");
        context.Request.Headers.Authorization = $"Bearer {invalidToken}";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_MissingToken_Returns401()
    {
        // Arrange
        var options = CreateOptions();
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        // No Authorization header

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_MalformedToken_Returns401()
    {
        // Arrange
        var options = CreateOptions();
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Request.Headers.Authorization = "Bearer not-a-valid-jwt-token";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_NoSigningKeyConfigured_Returns401()
    {
        // Arrange
        var options = CreateOptions(signingKey: null);
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Request.Headers.Authorization = $"Bearer {GenerateValidToken()}";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_EmptySigningKeyConfigured_Returns401()
    {
        // Arrange
        var options = CreateOptions(signingKey: "");
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Request.Headers.Authorization = $"Bearer {GenerateValidToken()}";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShortSigningKey_Returns401()
    {
        // Arrange - key less than 32 characters
        var options = CreateOptions(signingKey: "short-key-only-25-chars!");
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Request.Headers.Authorization = $"Bearer {GenerateValidToken()}";

        // Act
        await middleware.InvokeAsync(context);

        // Assert - Should reject because key is too short for secure HMAC-SHA256
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_AnonymousEndpoint_SkipsAuthentication()
    {
        // Arrange
        var options = CreateOptions();
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/health";
        // No Authorization header

        var nextCalled = false;
        _nextMock.Setup(n => n(It.IsAny<HttpContext>()))
            .Callback(() => nextCalled = true)
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_AuthNotRequired_SkipsAuthentication()
    {
        // Arrange
        var options = CreateOptions(requireAuth: false);
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        // No Authorization header

        var nextCalled = false;
        _nextMock.Setup(n => n(It.IsAny<HttpContext>()))
            .Callback(() => nextCalled = true)
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_JwtDisabled_DoesNotValidateToken()
    {
        // Arrange - JWT disabled, only API key enabled (but no keys)
        var options = new ApiAuthenticationOptions
        {
            EnableJwtAuthentication = false,
            EnableApiKeyAuthentication = false,
            RequireAuthentication = true,
            AnonymousEndpoints = Array.Empty<string>()
        };

        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Request.Headers.Authorization = $"Bearer {GenerateValidToken()}";

        // Act
        await middleware.InvokeAsync(context);

        // Assert - Should return 401 because JWT validation is disabled
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ValidateLifetimeDisabled_AcceptsExpiredToken()
    {
        // Arrange
        var options = CreateOptions(validateLifetime: false);
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";

        // Create token that expired 1 hour ago
        var expiredToken = GenerateValidToken(expires: DateTime.UtcNow.AddHours(-1));
        context.Request.Headers.Authorization = $"Bearer {expiredToken}";

        var nextCalled = false;
        _nextMock.Setup(n => n(It.IsAny<HttpContext>()))
            .Callback(() => nextCalled = true)
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - Should accept expired token when lifetime validation is disabled
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_NoIssuerConfigured_SkipsIssuerValidation()
    {
        // Arrange
        var options = CreateOptions(issuer: "");
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";

        // Generate token with any issuer
        var token = GenerateValidToken(issuer: "any-issuer");
        context.Request.Headers.Authorization = $"Bearer {token}";

        var nextCalled = false;
        _nextMock.Setup(n => n(It.IsAny<HttpContext>()))
            .Callback(() => nextCalled = true)
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - Should accept token with any issuer
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_NoAudienceConfigured_SkipsAudienceValidation()
    {
        // Arrange
        var options = CreateOptions(audience: "");
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";

        // Generate token with any audience
        var token = GenerateValidToken(audience: "any-audience");
        context.Request.Headers.Authorization = $"Bearer {token}";

        var nextCalled = false;
        _nextMock.Setup(n => n(It.IsAny<HttpContext>()))
            .Callback(() => nextCalled = true)
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - Should accept token with any audience
        Assert.True(nextCalled);
    }
}
