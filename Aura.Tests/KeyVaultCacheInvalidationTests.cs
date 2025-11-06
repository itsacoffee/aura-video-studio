using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Controllers;
using Aura.Api.Models;
using Aura.Core.Configuration;
using Aura.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests to verify KeyStore cache is properly invalidated when API keys are saved/rotated/deleted
/// This prevents the bug where validation fails immediately after saving a new key
/// </summary>
public class KeyVaultCacheInvalidationTests
{
    private static KeyVaultController CreateController(
        out Mock<IKeyStore> mockKeyStore,
        out Mock<ISecureStorageService> mockSecureStorage)
    {
        var mockLogger = new Mock<ILogger<KeyVaultController>>();
        mockSecureStorage = new Mock<ISecureStorageService>();
        var mockKeyValidator = new Mock<IKeyValidationService>();
        mockKeyStore = new Mock<IKeyStore>();
        
        var controller = new KeyVaultController(
            mockLogger.Object,
            mockSecureStorage.Object,
            mockKeyValidator.Object,
            mockKeyStore.Object);
        
        // Mock HttpContext to avoid NullReferenceException
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        
        return controller;
    }
    
    [Fact]
    public async Task SetApiKey_InvalidatesKeyStoreCache()
    {
        // Arrange
        var controller = CreateController(out var mockKeyStore, out var mockSecureStorage);
        
        var request = new SetApiKeyRequest
        {
            Provider = "openai",
            ApiKey = "sk-test123"
        };
        
        mockSecureStorage
            .Setup(x => x.SaveApiKeyAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        
        // Act
        var result = await controller.SetApiKey(request, CancellationToken.None);
        
        // Assert
        Assert.IsType<OkObjectResult>(result);
        mockKeyStore.Verify(x => x.Reload(), Times.Once, 
            "KeyStore.Reload() should be called after saving API key to invalidate cache");
    }
    
    [Fact]
    public async Task RotateApiKey_InvalidatesKeyStoreCache()
    {
        // Arrange
        var controller = CreateController(out var mockKeyStore, out var mockSecureStorage);
        
        var request = new RotateApiKeyRequest
        {
            Provider = "openai",
            NewApiKey = "sk-test456",
            TestBeforeSaving = false
        };
        
        mockSecureStorage
            .Setup(x => x.HasApiKeyAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        
        mockSecureStorage
            .Setup(x => x.SaveApiKeyAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        
        // Act
        var result = await controller.RotateApiKey(request, CancellationToken.None);
        
        // Assert
        Assert.IsType<OkObjectResult>(result);
        mockKeyStore.Verify(x => x.Reload(), Times.Once,
            "KeyStore.Reload() should be called after rotating API key to invalidate cache");
    }
    
    [Fact]
    public async Task DeleteApiKey_InvalidatesKeyStoreCache()
    {
        // Arrange
        var controller = CreateController(out var mockKeyStore, out var mockSecureStorage);
        
        var provider = "openai";
        
        mockSecureStorage
            .Setup(x => x.HasApiKeyAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        
        mockSecureStorage
            .Setup(x => x.DeleteApiKeyAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        
        // Act
        var result = await controller.DeleteApiKey(provider, CancellationToken.None);
        
        // Assert
        Assert.IsType<OkObjectResult>(result);
        mockKeyStore.Verify(x => x.Reload(), Times.Once,
            "KeyStore.Reload() should be called after deleting API key to invalidate cache");
    }
}
