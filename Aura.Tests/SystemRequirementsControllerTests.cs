using System;
using System.Threading.Tasks;
using Aura.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Integration tests for SystemRequirementsController
/// </summary>
public class SystemRequirementsControllerTests
{
    private readonly SystemRequirementsController _controller;

    public SystemRequirementsControllerTests()
    {
        var logger = new LoggerFactory().CreateLogger<SystemRequirementsController>();
        _controller = new SystemRequirementsController(logger);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public void GetDiskSpace_ReturnsOkResult()
    {
        var result = _controller.GetDiskSpace();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        var value = okResult.Value;
        var totalGBProperty = value.GetType().GetProperty("totalGB");
        var availableGBProperty = value.GetType().GetProperty("availableGB");
        
        Assert.NotNull(totalGBProperty);
        Assert.NotNull(availableGBProperty);
        
        var totalGB = (double)totalGBProperty.GetValue(value);
        var availableGB = (double)availableGBProperty.GetValue(value);
        
        Assert.True(totalGB > 0);
        Assert.True(availableGB >= 0);
        Assert.True(availableGB <= totalGB);
    }

    [Fact]
    public void GetGPUInfo_ReturnsOkResult()
    {
        var result = _controller.GetGPUInfo();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        var value = okResult.Value;
        var detectedProperty = value.GetType().GetProperty("detected");
        var vendorProperty = value.GetType().GetProperty("vendor");
        
        Assert.NotNull(detectedProperty);
        Assert.NotNull(vendorProperty);
    }

    [Fact]
    public void GetMemoryInfo_ReturnsOkResult()
    {
        var result = _controller.GetMemoryInfo();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        var value = okResult.Value;
        var totalGBProperty = value.GetType().GetProperty("totalGB");
        var availableGBProperty = value.GetType().GetProperty("availableGB");
        
        Assert.NotNull(totalGBProperty);
        Assert.NotNull(availableGBProperty);
    }

    [Fact]
    public async Task GetSystemRequirements_ReturnsOkResult()
    {
        var result = await _controller.GetSystemRequirements(default);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        var value = okResult.Value;
        var overallProperty = value.GetType().GetProperty("overall");
        var diskSpaceProperty = value.GetType().GetProperty("diskSpace");
        var gpuProperty = value.GetType().GetProperty("gpu");
        var memoryProperty = value.GetType().GetProperty("memory");
        var osProperty = value.GetType().GetProperty("os");
        
        Assert.NotNull(overallProperty);
        Assert.NotNull(diskSpaceProperty);
        Assert.NotNull(gpuProperty);
        Assert.NotNull(memoryProperty);
        Assert.NotNull(osProperty);
        
        var overall = (string)overallProperty.GetValue(value);
        Assert.Contains(overall, new[] { "pass", "warning", "fail" });
    }

    [Fact]
    public async Task GetSystemRequirements_ContainsDiskSpaceInfo()
    {
        var result = await _controller.GetSystemRequirements(default);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        
        var diskSpaceProperty = value.GetType().GetProperty("diskSpace");
        var diskSpace = diskSpaceProperty.GetValue(value);
        
        var availableProperty = diskSpace.GetType().GetProperty("available");
        var totalProperty = diskSpace.GetType().GetProperty("total");
        var statusProperty = diskSpace.GetType().GetProperty("status");
        
        Assert.NotNull(availableProperty);
        Assert.NotNull(totalProperty);
        Assert.NotNull(statusProperty);
        
        var status = (string)statusProperty.GetValue(diskSpace);
        Assert.Contains(status, new[] { "pass", "warning", "fail" });
    }

    [Fact]
    public async Task GetSystemRequirements_ContainsGPUInfo()
    {
        var result = await _controller.GetSystemRequirements(default);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        
        var gpuProperty = value.GetType().GetProperty("gpu");
        var gpu = gpuProperty.GetValue(value);
        
        var detectedProperty = gpu.GetType().GetProperty("detected");
        var capabilitiesProperty = gpu.GetType().GetProperty("capabilities");
        var statusProperty = gpu.GetType().GetProperty("status");
        
        Assert.NotNull(detectedProperty);
        Assert.NotNull(capabilitiesProperty);
        Assert.NotNull(statusProperty);
    }

    [Fact]
    public async Task GetSystemRequirements_ContainsMemoryInfo()
    {
        var result = await _controller.GetSystemRequirements(default);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        
        var memoryProperty = value.GetType().GetProperty("memory");
        var memory = memoryProperty.GetValue(value);
        
        var totalProperty = memory.GetType().GetProperty("total");
        var availableProperty = memory.GetType().GetProperty("available");
        var statusProperty = memory.GetType().GetProperty("status");
        var warningsProperty = memory.GetType().GetProperty("warnings");
        
        Assert.NotNull(totalProperty);
        Assert.NotNull(availableProperty);
        Assert.NotNull(statusProperty);
        Assert.NotNull(warningsProperty);
    }

    [Fact]
    public async Task GetSystemRequirements_ContainsOSInfo()
    {
        var result = await _controller.GetSystemRequirements(default);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        
        var osProperty = value.GetType().GetProperty("os");
        var os = osProperty.GetValue(value);
        
        var platformProperty = os.GetType().GetProperty("platform");
        var versionProperty = os.GetType().GetProperty("version");
        var architectureProperty = os.GetType().GetProperty("architecture");
        var compatibleProperty = os.GetType().GetProperty("compatible");
        
        Assert.NotNull(platformProperty);
        Assert.NotNull(versionProperty);
        Assert.NotNull(architectureProperty);
        Assert.NotNull(compatibleProperty);
        
        var platform = (string)platformProperty.GetValue(os);
        Assert.NotEmpty(platform);
    }

    [Fact]
    public void DiskSpace_ProvidesPercentageCalculation()
    {
        var result = _controller.GetDiskSpace();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        
        var percentageFreeProperty = value.GetType().GetProperty("percentageFree");
        Assert.NotNull(percentageFreeProperty);
        
        var percentageFree = (double)percentageFreeProperty.GetValue(value);
        Assert.True(percentageFree >= 0);
        Assert.True(percentageFree <= 100);
    }

    [Fact]
    public void GPUInfo_IncludesCapabilities()
    {
        var result = _controller.GetGPUInfo();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        
        var hardwareAccelerationProperty = value.GetType().GetProperty("hardwareAcceleration");
        var videoEncodingProperty = value.GetType().GetProperty("videoEncoding");
        var videoDecodingProperty = value.GetType().GetProperty("videoDecoding");
        
        Assert.NotNull(hardwareAccelerationProperty);
        Assert.NotNull(videoEncodingProperty);
        Assert.NotNull(videoDecodingProperty);
    }
}
