using System;
using System.Collections.Generic;
using System.Text.Json;
using Aura.Core.Services.Diagnostics;
using Xunit;

namespace Aura.Tests;

public class RedactionServiceTests
{
    [Fact]
    public void RedactText_RedactsOpenAIKeys()
    {
        // Arrange
        var content = "Using API key sk-1234567890abcdefghijklmnopqrstuvwxyz123456 for request";

        // Act
        var redacted = RedactionService.RedactText(content);

        // Assert
        Assert.DoesNotContain("sk-1234567890", redacted);
        Assert.Contains("[REDACTED]", redacted);
    }

    [Fact]
    public void RedactText_RedactsOpenAIProjectKeys()
    {
        // Arrange
        var content = "Project key: sk-proj-abcdefghijklmnopqrstuvwxyz123456789";

        // Act
        var redacted = RedactionService.RedactText(content);

        // Assert
        Assert.DoesNotContain("sk-proj-abcdefghijklmnopqrstuvwxyz123456789", redacted);
        Assert.Contains("[REDACTED]", redacted);
    }

    [Fact]
    public void RedactText_RedactsAnthropicKeys()
    {
        // Arrange
        var content = "Anthropic key: sk-ant-api03-1234567890abcdefghijklmnopqrstuvwxyz";

        // Act
        var redacted = RedactionService.RedactText(content);

        // Assert
        Assert.DoesNotContain("sk-ant-api03-1234567890", redacted);
        Assert.Contains("[REDACTED]", redacted);
    }

    [Fact]
    public void RedactText_RedactsBearerTokens()
    {
        // Arrange
        var content = "Authorization: Bearer abc123def456ghi789jkl012mno345pqr678";

        // Act
        var redacted = RedactionService.RedactText(content);

        // Assert
        Assert.DoesNotContain("abc123def456ghi789", redacted);
        Assert.Contains("[REDACTED]", redacted);
    }

    [Fact]
    public void RedactText_RedactsJWTTokens()
    {
        // Arrange
        var content = "JWT: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

        // Act
        var redacted = RedactionService.RedactText(content);

        // Assert
        Assert.DoesNotContain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9", redacted);
        Assert.Contains("[REDACTED]", redacted);
    }

    [Fact]
    public void RedactText_RedactsGoogleAPIKeys()
    {
        // Arrange
        var content = "Google key: AIzaSyDxJ2L4Yk9w8r5t6u7i8o9p0a1s2d3f4g5h6";

        // Act
        var redacted = RedactionService.RedactText(content);

        // Assert
        Assert.DoesNotContain("AIzaSyDxJ2L4Yk9w8r5t6u7i8o9p0a1s2d3f4g5h6", redacted);
        Assert.Contains("[REDACTED]", redacted);
    }

    [Fact]
    public void RedactText_RedactsAWSKeys()
    {
        // Arrange
        var content = "AWS key: AKIAIOSFODNN7EXAMPLE";

        // Act
        var redacted = RedactionService.RedactText(content);

        // Assert
        Assert.DoesNotContain("AKIAIOSFODNN7EXAMPLE", redacted);
        Assert.Contains("[REDACTED]", redacted);
    }

    [Fact]
    public void RedactText_RedactsGitHubTokens()
    {
        // Arrange
        var content = "GitHub token: ghp_1234567890abcdefghijklmnopqrstuvwxyz";

        // Act
        var redacted = RedactionService.RedactText(content);

        // Assert
        Assert.DoesNotContain("ghp_1234567890abcdefghijklmnopqrstuvwxyz", redacted);
        Assert.Contains("[REDACTED]", redacted);
    }

    [Fact]
    public void RedactText_RedactsReplicateTokens()
    {
        // Arrange
        var content = "Replicate token: r8_1234567890abcdefghijklmnopqrstuvwxyzABCDEF";

        // Act
        var redacted = RedactionService.RedactText(content);

        // Assert
        Assert.DoesNotContain("r8_1234567890abcdefghijklmnopqrstuvwxyzABCDEF", redacted);
        Assert.Contains("[REDACTED]", redacted);
    }

    [Fact]
    public void RedactText_RedactsJSONAPIKeyField()
    {
        // Arrange
        var json = "{\"apiKey\": \"sk-1234567890abcdef\", \"provider\": \"OpenAI\"}";

        // Act
        var redacted = RedactionService.RedactText(json);

        // Assert
        Assert.DoesNotContain("sk-1234567890abcdef", redacted);
        Assert.Contains("\"apiKey\": \"[REDACTED]\"", redacted);
        Assert.Contains("\"provider\": \"OpenAI\"", redacted); // Provider should not be redacted
    }

    [Fact]
    public void RedactText_RedactsPasswordField()
    {
        // Arrange
        var json = "{\"password\": \"secret123\", \"username\": \"user\"}";

        // Act
        var redacted = RedactionService.RedactText(json);

        // Assert
        Assert.DoesNotContain("secret123", redacted);
        Assert.Contains("\"password\": \"[REDACTED]\"", redacted);
        Assert.Contains("\"username\": \"user\"", redacted); // Username should not be redacted
    }

    [Fact]
    public void RedactText_PreservesNonSensitiveContent()
    {
        // Arrange
        var content = "Job failed with error code E429 at stage TTS. Provider: ElevenLabs";

        // Act
        var redacted = RedactionService.RedactText(content);

        // Assert
        Assert.Equal(content, redacted); // No redaction should occur
    }

    [Fact]
    public void RedactLogLines_FiltersTimeWindow()
    {
        // Arrange
        var failureTime = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var lines = new List<string>
        {
            "2024-01-15T11:54:00Z [INFO] Starting job",  // Before window
            "2024-01-15T11:56:00Z [INFO] Processing stage 1",  // Within window
            "2024-01-15T12:00:00Z [ERROR] Job failed",  // Failure time
            "2024-01-15T12:04:00Z [INFO] Cleanup started",  // Within window
            "2024-01-15T12:10:00Z [INFO] Job complete"  // After window
        };

        // Act
        var redacted = RedactionService.RedactLogLines(lines, failureTime, TimeSpan.FromMinutes(5));

        // Assert
        var redactedList = new List<string>(redacted);
        Assert.Equal(3, redactedList.Count); // Only lines within ±5 minutes
        Assert.Contains("11:56:00", redactedList[0]);
        Assert.Contains("12:00:00", redactedList[1]);
        Assert.Contains("12:04:00", redactedList[2]);
    }

    [Fact]
    public void RedactLogLines_WithoutWindow_ReturnsAllLines()
    {
        // Arrange
        var lines = new List<string>
        {
            "Line 1 with sk-1234567890abcdefghijklmnopqrstuvwxyz",
            "Line 2 normal text",
            "Line 3 with token ghp_abcdefghijklmnopqrstuvwxyz123456"
        };

        // Act
        var redacted = RedactionService.RedactLogLines(lines);

        // Assert
        var redactedList = new List<string>(redacted);
        Assert.Equal(3, redactedList.Count);
        Assert.DoesNotContain("sk-1234567890", redactedList[0]);
        Assert.Contains("[REDACTED]", redactedList[0]);
        Assert.DoesNotContain("ghp_abcdefghijklmnopqrstuvwxyz123456", redactedList[2]);
    }

    [Fact]
    public void GetAllowedFields_ReturnsExpectedFields()
    {
        // Act
        var allowedFields = RedactionService.GetAllowedFields();

        // Assert
        Assert.Contains("jobId", allowedFields);
        Assert.Contains("correlationId", allowedFields);
        Assert.Contains("timestamp", allowedFields);
        Assert.Contains("stage", allowedFields);
        Assert.Contains("provider", allowedFields);
        Assert.Contains("cost", allowedFields);
        Assert.Contains("latency", allowedFields);
        
        // Should NOT contain sensitive fields
        Assert.DoesNotContain("apiKey", allowedFields);
        Assert.DoesNotContain("password", allowedFields);
        Assert.DoesNotContain("token", allowedFields);
    }

    [Fact]
    public void RedactText_PreservesCorrelationIDs()
    {
        // Arrange
        var content = "Request abc-123-def-456 with correlation ID xyz-789-uvw-012";

        // Act
        var redacted = RedactionService.RedactText(content);

        // Assert
        Assert.Contains("abc-123-def-456", redacted);
        Assert.Contains("xyz-789-uvw-012", redacted);
    }

    [Fact]
    public void RedactText_HandlesMixedContent()
    {
        // Arrange
        var content = @"
Job job-123 started at 2024-01-15T12:00:00Z
Using provider OpenAI with key sk-1234567890abcdefghijklmnopqrstuvwxyz
Cost: $0.045, Latency: 2500ms
Bearer token: Bearer xyz789abc456def123ghi
Error: Rate limit exceeded
Correlation ID: abc-def-ghi-123
";

        // Act
        var redacted = RedactionService.RedactText(content);

        // Assert
        // Should preserve
        Assert.Contains("job-123", redacted);
        Assert.Contains("OpenAI", redacted);
        Assert.Contains("$0.045", redacted);
        Assert.Contains("2500ms", redacted);
        Assert.Contains("Rate limit exceeded", redacted);
        Assert.Contains("abc-def-ghi-123", redacted);
        
        // Should redact
        Assert.DoesNotContain("sk-1234567890", redacted);
        Assert.DoesNotContain("xyz789abc456def123ghi", redacted);
        Assert.Contains("[REDACTED]", redacted);
    }
}
