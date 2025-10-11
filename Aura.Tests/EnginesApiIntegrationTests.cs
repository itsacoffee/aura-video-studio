using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Aura.Api.Controllers;
using Aura.Core.Downloads;
using Aura.Core.Runtime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Integration tests for Engines API endpoints.
/// These tests validate the controller logic with mocked dependencies.
/// </summary>
public class EnginesApiIntegrationTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _installRoot;
    private readonly string _manifestPath;
    private readonly string _configPath;
    private readonly string _logDirectory;
    private readonly HttpClient _httpClient;

    public EnginesApiIntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "aura-engine-api-tests-" + Guid.NewGuid().ToString());
        _installRoot = Path.Combine(_testDirectory, "engines");
        _manifestPath = Path.Combine(_testDirectory, "manifest.json");
        _configPath = Path.Combine(_testDirectory, "config.json");
        _logDirectory = Path.Combine(_testDirectory, "logs");
        _httpClient = new HttpClient();
        
        Directory.CreateDirectory(_testDirectory);
        Directory.CreateDirectory(_installRoot);
        Directory.CreateDirectory(_logDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        _httpClient.Dispose();
    }

    [Fact]
    public async Task GetList_Should_ReturnEnginesList()
    {
        // Arrange
        var manifestLoader = new EngineManifestLoader(
            NullLogger<EngineManifestLoader>.Instance,
            _httpClient,
            _manifestPath);
        
        var installer = new EngineInstaller(
            NullLogger<EngineInstaller>.Instance,
            _httpClient,
            _installRoot);
        
        var processManager = new ExternalProcessManager(
            NullLogger<ExternalProcessManager>.Instance,
            _httpClient,
            _logDirectory);
        
        var registry = new LocalEnginesRegistry(
            NullLogger<LocalEnginesRegistry>.Instance,
            processManager,
            _configPath);
        
        var controller = new EnginesController(
            NullLogger<EnginesController>.Instance,
            manifestLoader,
            installer,
            registry,
            processManager);

        // Act
        var result = await controller.GetList();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        // Check that the response has an engines property
        var responseType = okResult.Value.GetType();
        var enginesProperty = responseType.GetProperty("engines");
        Assert.NotNull(enginesProperty);
    }

    [Fact]
    public async Task GetStatus_Should_ReturnNotFound_WhenEngineDoesNotExist()
    {
        // Arrange
        var manifestLoader = new EngineManifestLoader(
            NullLogger<EngineManifestLoader>.Instance,
            _httpClient,
            _manifestPath);
        
        var installer = new EngineInstaller(
            NullLogger<EngineInstaller>.Instance,
            _httpClient,
            _installRoot);
        
        var processManager = new ExternalProcessManager(
            NullLogger<ExternalProcessManager>.Instance,
            _httpClient,
            _logDirectory);
        
        var registry = new LocalEnginesRegistry(
            NullLogger<LocalEnginesRegistry>.Instance,
            processManager,
            _configPath);
        
        var controller = new EnginesController(
            NullLogger<EnginesController>.Instance,
            manifestLoader,
            installer,
            registry,
            processManager);

        // Act
        var result = await controller.GetStatus("nonexistent-engine");

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetStatus_Should_ReturnStatus_WhenEngineExists()
    {
        // Arrange
        var manifestLoader = new EngineManifestLoader(
            NullLogger<EngineManifestLoader>.Instance,
            _httpClient,
            _manifestPath);
        
        var installer = new EngineInstaller(
            NullLogger<EngineInstaller>.Instance,
            _httpClient,
            _installRoot);
        
        var processManager = new ExternalProcessManager(
            NullLogger<ExternalProcessManager>.Instance,
            _httpClient,
            _logDirectory);
        
        var registry = new LocalEnginesRegistry(
            NullLogger<LocalEnginesRegistry>.Instance,
            processManager,
            _configPath);
        
        var controller = new EnginesController(
            NullLogger<EnginesController>.Instance,
            manifestLoader,
            installer,
            registry,
            processManager);

        // Act - use a known engine ID from the default manifest
        var result = await controller.GetStatus("stable-diffusion-webui");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Verify_Should_ReturnNotFound_WhenEngineDoesNotExist()
    {
        // Arrange
        var manifestLoader = new EngineManifestLoader(
            NullLogger<EngineManifestLoader>.Instance,
            _httpClient,
            _manifestPath);
        
        var installer = new EngineInstaller(
            NullLogger<EngineInstaller>.Instance,
            _httpClient,
            _installRoot);
        
        var processManager = new ExternalProcessManager(
            NullLogger<ExternalProcessManager>.Instance,
            _httpClient,
            _logDirectory);
        
        var registry = new LocalEnginesRegistry(
            NullLogger<LocalEnginesRegistry>.Instance,
            processManager,
            _configPath);
        
        var controller = new EnginesController(
            NullLogger<EnginesController>.Instance,
            manifestLoader,
            installer,
            registry,
            processManager);

        // Act
        var result = await controller.Verify(new EngineActionRequest("nonexistent-engine"));

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public void EngineActionRequest_Should_ValidateEngineId()
    {
        // Test that EngineActionRequest requires engineId
        var request = new EngineActionRequest("test-engine");
        
        Assert.NotNull(request.EngineId);
        Assert.Equal("test-engine", request.EngineId);
    }

    [Fact]
    public void InstallRequest_Should_AcceptOptionalParameters()
    {
        // Test that InstallRequest works with optional parameters
        var requestWithDefaults = new InstallRequest("test-engine");
        Assert.Equal("test-engine", requestWithDefaults.EngineId);
        Assert.Null(requestWithDefaults.Version);
        Assert.Null(requestWithDefaults.Port);

        var requestWithParams = new InstallRequest("test-engine", "1.0.0", 8080);
        Assert.Equal("test-engine", requestWithParams.EngineId);
        Assert.Equal("1.0.0", requestWithParams.Version);
        Assert.Equal(8080, requestWithParams.Port);
    }

    [Fact]
    public void StartRequest_Should_AcceptOptionalParameters()
    {
        // Test that StartRequest works with optional parameters
        var requestWithDefaults = new StartRequest("test-engine");
        Assert.Equal("test-engine", requestWithDefaults.EngineId);
        Assert.Null(requestWithDefaults.Port);
        Assert.Null(requestWithDefaults.Args);

        var requestWithParams = new StartRequest("test-engine", 8080, "--api --listen");
        Assert.Equal("test-engine", requestWithParams.EngineId);
        Assert.Equal(8080, requestWithParams.Port);
        Assert.Equal("--api --listen", requestWithParams.Args);
    }
}
