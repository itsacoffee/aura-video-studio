using Aura.Core.Services;
using System.Collections.Generic;
using Xunit;

namespace Aura.Tests;

public class SecretMaskingServiceTests
{
    [Fact]
    public void MaskApiKey_ShortKey_ReturnsMasked()
    {
        var key = "abc123";
        var result = SecretMaskingService.MaskApiKey(key);
        
        Assert.Equal("***", result);
    }
    
    [Fact]
    public void MaskApiKey_LongKey_ShowsStartAndEnd()
    {
        var key = "sk-1234567890abcdefghijklmnopqrstuvwxyz";
        var result = SecretMaskingService.MaskApiKey(key);
        
        Assert.StartsWith("sk-12345", result);
        Assert.EndsWith("wxyz", result);
        Assert.Contains("...", result);
    }
    
    [Fact]
    public void MaskApiKey_NullOrEmpty_ReturnsNotSet()
    {
        Assert.Equal("[not set]", SecretMaskingService.MaskApiKey(null));
        Assert.Equal("[not set]", SecretMaskingService.MaskApiKey(""));
        Assert.Equal("[not set]", SecretMaskingService.MaskApiKey("   "));
    }
    
    [Fact]
    public void IsSensitiveKey_ApiKeyNames_ReturnsTrue()
    {
        Assert.True(SecretMaskingService.IsSensitiveKey("apiKey"));
        Assert.True(SecretMaskingService.IsSensitiveKey("api_key"));
        Assert.True(SecretMaskingService.IsSensitiveKey("api-key"));
        Assert.True(SecretMaskingService.IsSensitiveKey("OpenAI_ApiKey"));
    }
    
    [Fact]
    public void IsSensitiveKey_SecretNames_ReturnsTrue()
    {
        Assert.True(SecretMaskingService.IsSensitiveKey("secret"));
        Assert.True(SecretMaskingService.IsSensitiveKey("password"));
        Assert.True(SecretMaskingService.IsSensitiveKey("token"));
    }
    
    [Fact]
    public void IsSensitiveKey_NonSensitiveNames_ReturnsFalse()
    {
        Assert.False(SecretMaskingService.IsSensitiveKey("username"));
        Assert.False(SecretMaskingService.IsSensitiveKey("url"));
        Assert.False(SecretMaskingService.IsSensitiveKey("model"));
    }
    
    [Fact]
    public void MaskSecretsInText_FindsAndMasksKeys()
    {
        var text = "Using OpenAI with key sk-1234567890abcdefghijklmnopqrstuvwxyz for requests";
        var result = SecretMaskingService.MaskSecretsInText(text);
        
        Assert.DoesNotContain("sk-1234567890abcdefghijklmnopqrstuvwxyz", result);
        Assert.Contains("...", result);
    }
    
    [Fact]
    public void MaskDictionary_MasksOnlySensitiveKeys()
    {
        var dict = new Dictionary<string, string>
        {
            ["apiKey"] = "sk-secret123",
            ["url"] = "https://api.example.com",
            ["password"] = "mypassword"
        };
        
        var masked = SecretMaskingService.MaskDictionary(dict);
        
        Assert.NotEqual("sk-secret123", masked["apiKey"]);
        Assert.Equal("https://api.example.com", masked["url"]);
        Assert.NotEqual("mypassword", masked["password"]);
    }
}
