using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Diagnostics;
using Aura.Core.Services.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class DiagnosticBundleServiceTests
{
    private readonly Mock<ILogger<DiagnosticBundleService>> _mockLogger;
    private readonly Mock<DiagnosticReportGenerator> _mockReportGenerator;

    public DiagnosticBundleServiceTests()
    {
        _mockLogger = new Mock<ILogger<DiagnosticBundleService>>();
        _mockReportGenerator = new Mock<DiagnosticReportGenerator>(MockBehavior.Loose, null);
    }

    [Fact]
    public void RedactSensitiveData_RedactsOpenAIKeys()
    {
        // Arrange
        var content = "Using API key sk-1234567890abcdefghijklmnopqrstuvwxyz123456 for request";

        // Act
        var redacted = DiagnosticBundleService.RedactSensitiveData(content);

        // Assert
        Assert.DoesNotContain("sk-1234567890", redacted);
        Assert.Contains("[REDACTED-API-KEY]", redacted);
    }

    [Fact]
    public void RedactSensitiveData_RedactsOpenAIProjectKeys()
    {
        // Arrange
        var content = "Project key: sk-proj-abcdefghijklmnopqrstuvwxyz123456789";

        // Act
        var redacted = DiagnosticBundleService.RedactSensitiveData(content);

        // Assert
        Assert.DoesNotContain("sk-proj-abcdefghijklmnopqrstuvwxyz123456789", redacted);
        Assert.Contains("[REDACTED-API-KEY]", redacted);
    }

    [Fact]
    public void RedactSensitiveData_RedactsAnthropicKeys()
    {
        // Arrange
        var content = "Anthropic key: sk-ant-api03-1234567890abcdefghijklmnopqrstuvwxyz";

        // Act
        var redacted = DiagnosticBundleService.RedactSensitiveData(content);

        // Assert
        Assert.DoesNotContain("sk-ant-api03-1234567890", redacted);
        Assert.Contains("[REDACTED-API-KEY]", redacted);
    }

    [Fact]
    public void RedactSensitiveData_RedactsBearerTokens()
    {
        // Arrange
        var content = "Authorization: Bearer abc123def456ghi789jkl012mno345pqr678";

        // Act
        var redacted = DiagnosticBundleService.RedactSensitiveData(content);

        // Assert
        Assert.DoesNotContain("abc123def456ghi789", redacted);
        Assert.Contains("Bearer [REDACTED-TOKEN]", redacted);
    }

    [Fact]
    public void RedactSensitiveData_RedactsJWTTokens()
    {
        // Arrange
        var content = "JWT: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

        // Act
        var redacted = DiagnosticBundleService.RedactSensitiveData(content);

        // Assert
        Assert.DoesNotContain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9", redacted);
        Assert.Contains("[REDACTED-JWT]", redacted);
    }

    [Fact]
    public void RedactSensitiveData_RedactsGoogleAPIKeys()
    {
        // Arrange
        var content = "Google API key: AIzaSyD1234567890abcdefghijklmnopqrstuv";

        // Act
        var redacted = DiagnosticBundleService.RedactSensitiveData(content);

        // Assert
        Assert.DoesNotContain("AIzaSyD1234567890", redacted);
        Assert.Contains("[REDACTED-API-KEY]", redacted);
    }

    [Fact]
    public void RedactSensitiveData_RedactsAWSKeys()
    {
        // Arrange
        var content = "AWS Access Key: AKIAIOSFODNN7EXAMPLE";

        // Act
        var redacted = DiagnosticBundleService.RedactSensitiveData(content);

        // Assert
        Assert.DoesNotContain("AKIAIOSFODNN7EXAMPLE", redacted);
        Assert.Contains("[REDACTED-AWS-KEY]", redacted);
    }

    [Fact]
    public void RedactSensitiveData_RedactsGitHubTokens()
    {
        // Arrange
        var content = "GitHub token: ghp_1234567890abcdefghijklmnopqrstuvwxyz12";

        // Act
        var redacted = DiagnosticBundleService.RedactSensitiveData(content);

        // Assert
        Assert.DoesNotContain("ghp_1234567890abcdefghijklmnopqrstuvwxyz12", redacted);
        Assert.Contains("[REDACTED-GH-TOKEN]", redacted);
    }

    [Fact]
    public void RedactSensitiveData_RedactsJSONApiKeys()
    {
        // Arrange
        var content = @"{""apiKey"": ""secret-key-12345"", ""other"": ""value""}";

        // Act
        var redacted = DiagnosticBundleService.RedactSensitiveData(content);

        // Assert
        Assert.DoesNotContain("secret-key-12345", redacted);
        Assert.Contains(@"""apiKey"": ""[REDACTED]""", redacted);
    }

    [Fact]
    public void RedactSensitiveData_RedactsJSONApiKeysUnderscoreFormat()
    {
        // Arrange
        var content = @"{""api_key"": ""secret-api-key-67890"", ""other"": ""value""}";

        // Act
        var redacted = DiagnosticBundleService.RedactSensitiveData(content);

        // Assert
        Assert.DoesNotContain("secret-api-key-67890", redacted);
        Assert.Contains(@"""api_key"": ""[REDACTED]""", redacted);
    }

    [Fact]
    public void RedactSensitiveData_RedactsPasswords()
    {
        // Arrange
        var content = @"{""password"": ""mySecretPassword123"", ""user"": ""john""}";

        // Act
        var redacted = DiagnosticBundleService.RedactSensitiveData(content);

        // Assert
        Assert.DoesNotContain("mySecretPassword123", redacted);
        Assert.Contains(@"""password"": ""[REDACTED]""", redacted);
    }

    [Fact]
    public void RedactSensitiveData_RedactsTokensInJSON()
    {
        // Arrange
        var content = @"{""token"": ""access-token-abc123def456""}";

        // Act
        var redacted = DiagnosticBundleService.RedactSensitiveData(content);

        // Assert
        Assert.DoesNotContain("access-token-abc123def456", redacted);
        Assert.Contains(@"""token"": ""[REDACTED]""", redacted);
    }

    [Fact]
    public void RedactSensitiveData_RedactsSecretsInJSON()
    {
        // Arrange
        var content = @"{""secret"": ""my-secret-value-xyz""}";

        // Act
        var redacted = DiagnosticBundleService.RedactSensitiveData(content);

        // Assert
        Assert.DoesNotContain("my-secret-value-xyz", redacted);
        Assert.Contains(@"""secret"": ""[REDACTED]""", redacted);
    }

    [Fact]
    public void RedactSensitiveData_RedactsXApiKeyHeader()
    {
        // Arrange
        var content = "x-api-key: my-special-api-key-12345678";

        // Act
        var redacted = DiagnosticBundleService.RedactSensitiveData(content);

        // Assert
        Assert.DoesNotContain("my-special-api-key-12345678", redacted);
        Assert.Contains("x-api-key: [REDACTED]", redacted);
    }

    [Fact]
    public void RedactSensitiveData_RedactsAuthorizationHeader()
    {
        // Arrange
        var content = "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9";

        // Act
        var redacted = DiagnosticBundleService.RedactSensitiveData(content);

        // Assert
        Assert.DoesNotContain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9", redacted);
        Assert.Contains("Authorization: [REDACTED]", redacted);
    }

    [Fact]
    public void RedactSensitiveData_PreservesNonSensitiveContent()
    {
        // Arrange
        var content = "This is a normal log message with no secrets. User: john@example.com";

        // Act
        var redacted = DiagnosticBundleService.RedactSensitiveData(content);

        // Assert
        Assert.Equal(content, redacted);
        Assert.Contains("john@example.com", redacted);
    }

    [Fact]
    public void RedactSensitiveData_HandlesMultipleSecretsInSameContent()
    {
        // Arrange
        var content = @"Request with sk-1234567890abcdefghijklmnopqrstuvwxyz and Bearer token123456789012345678901234567890 and apiKey: ""my-key-abc""";

        // Act
        var redacted = DiagnosticBundleService.RedactSensitiveData(content);

        // Assert
        Assert.DoesNotContain("sk-1234567890", redacted);
        Assert.DoesNotContain("token123456789012345678901234567890", redacted);
        Assert.DoesNotContain("my-key-abc", redacted);
        Assert.Contains("[REDACTED-API-KEY]", redacted);
        Assert.Contains("[REDACTED-TOKEN]", redacted);
    }

    [Fact]
    public void RedactSensitiveData_HandlesEmptyString()
    {
        // Arrange
        var content = string.Empty;

        // Act
        var redacted = DiagnosticBundleService.RedactSensitiveData(content);

        // Assert
        Assert.Equal(string.Empty, redacted);
    }

    [Fact]
    public void RedactSensitiveData_PreservesLogStructure()
    {
        // Arrange
        var content = @"[2024-01-01 12:00:00] INFO: Request started
[2024-01-01 12:00:01] DEBUG: Using API key sk-test1234567890abcdefghijklmnop
[2024-01-01 12:00:02] INFO: Request completed";

        // Act
        var redacted = DiagnosticBundleService.RedactSensitiveData(content);

        // Assert
        Assert.Contains("[2024-01-01 12:00:00] INFO: Request started", redacted);
        Assert.Contains("[2024-01-01 12:00:02] INFO: Request completed", redacted);
        Assert.DoesNotContain("sk-test1234567890", redacted);
        Assert.Contains("[REDACTED-API-KEY]", redacted);
    }

    [Fact]
    public async Task GenerateBundleAsync_CreatesValidBundle()
    {
        // Arrange
        var service = new DiagnosticBundleService(
            _mockLogger.Object,
            _mockReportGenerator.Object,
            null);

        var job = new Job
        {
            Id = "test-job-123",
            Status = JobStatus.Failed,
            Stage = "Rendering",
            StartedAt = DateTime.UtcNow.AddHours(-1),
            FinishedAt = DateTime.UtcNow,
            ErrorMessage = "Test error with API key sk-test123456789012345678901234567890",
            CorrelationId = "corr-123"
        };

        // Act
        var bundle = await service.GenerateBundleAsync(job, null, null, null, CancellationToken.None);

        // Assert
        Assert.NotNull(bundle);
        Assert.Equal(job.Id, bundle.JobId);
        Assert.True(File.Exists(bundle.FilePath));
        Assert.True(bundle.SizeBytes > 0);

        // Cleanup
        if (File.Exists(bundle.FilePath))
        {
            File.Delete(bundle.FilePath);
        }
    }

    [Fact]
    public void RedactSensitiveData_CaseInsensitiveForHeaders()
    {
        // Arrange
        var content = "X-API-KEY: test-key-123456789 and x-api-key: another-key-987654321";

        // Act
        var redacted = DiagnosticBundleService.RedactSensitiveData(content);

        // Assert
        Assert.DoesNotContain("test-key-123456789", redacted);
        Assert.DoesNotContain("another-key-987654321", redacted);
        Assert.Contains("[REDACTED]", redacted);
    }

    [Fact]
    public void RedactSensitiveData_RedactsReplicateKeys()
    {
        // Arrange
        var content = "Replicate API key: r8_1234567890abcdefghijklmnopqrstuvwxyz123456";

        // Act
        var redacted = DiagnosticBundleService.RedactSensitiveData(content);

        // Assert
        Assert.DoesNotContain("r8_1234567890", redacted);
        Assert.Contains("[REDACTED-API-KEY]", redacted);
    }
}
