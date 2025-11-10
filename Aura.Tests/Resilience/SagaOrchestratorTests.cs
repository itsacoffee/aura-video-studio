using Aura.Core.Resilience.Saga;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Resilience;

public class SagaOrchestratorTests
{
    private readonly Mock<ILogger<SagaOrchestrator>> _loggerMock;
    private readonly SagaOrchestrator _orchestrator;

    public SagaOrchestratorTests()
    {
        _loggerMock = new Mock<ILogger<SagaOrchestrator>>();
        _orchestrator = new SagaOrchestrator(_loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCompleteAllSteps_WhenAllSucceed()
    {
        // Arrange
        var context = new SagaContext { SagaName = "TestSaga" };
        var step1 = new TestSagaStep("Step1");
        var step2 = new TestSagaStep("Step2");
        var step3 = new TestSagaStep("Step3");

        // Act
        var result = await _orchestrator.ExecuteAsync(context, new[] { step1, step2, step3 });

        // Assert
        Assert.True(result.Success);
        Assert.Equal(SagaState.Completed, context.State);
        Assert.Equal(3, context.CompletedSteps.Count);
        Assert.True(step1.WasExecuted);
        Assert.True(step2.WasExecuted);
        Assert.True(step3.WasExecuted);
        Assert.False(step1.WasCompensated);
        Assert.False(step2.WasCompensated);
        Assert.False(step3.WasCompensated);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCompensate_WhenStepFails()
    {
        // Arrange
        var context = new SagaContext { SagaName = "TestSaga" };
        var step1 = new TestSagaStep("Step1");
        var step2 = new TestSagaStep("Step2") { ShouldFail = true };
        var step3 = new TestSagaStep("Step3");

        // Act
        var result = await _orchestrator.ExecuteAsync(context, new[] { step1, step2, step3 });

        // Assert
        Assert.False(result.Success);
        Assert.Equal(SagaState.Compensated, context.State);
        Assert.True(step1.WasExecuted);
        Assert.True(step2.WasExecuted);
        Assert.False(step3.WasExecuted); // Step3 should not execute
        Assert.True(step1.WasCompensated); // Step1 should be compensated
        Assert.False(step2.WasCompensated); // Step2 that failed is not compensated
        Assert.Equal("Step2", result.FailedAtStep);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRecordEvents()
    {
        // Arrange
        var context = new SagaContext { SagaName = "TestSaga" };
        var step = new TestSagaStep("TestStep");

        // Act
        await _orchestrator.ExecuteAsync(context, new[] { step });

        // Assert
        Assert.NotEmpty(context.Events);
        Assert.Contains(context.Events, e => e.EventType == "started");
        Assert.Contains(context.Events, e => e.EventType == "executing" && e.StepId == "TestStep");
        Assert.Contains(context.Events, e => e.EventType == "completed" && e.StepId == "TestStep");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleCancellation()
    {
        // Arrange
        var context = new SagaContext { SagaName = "TestSaga" };
        var step1 = new TestSagaStep("Step1");
        var step2 = new TestSagaStep("Step2");
        
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        var result = await _orchestrator.ExecuteAsync(context, new[] { step1, step2 }, cts.Token);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.IsType<OperationCanceledException>(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkipNonCompensatableSteps()
    {
        // Arrange
        var context = new SagaContext { SagaName = "TestSaga" };
        var step1 = new TestSagaStep("Step1");
        var step2 = new TestSagaStep("Step2") { CanCompensate = false };
        var step3 = new TestSagaStep("Step3") { ShouldFail = true };

        // Act
        var result = await _orchestrator.ExecuteAsync(context, new[] { step1, step2, step3 });

        // Assert
        Assert.False(result.Success);
        Assert.True(step1.WasCompensated);
        Assert.False(step2.WasCompensated); // Can't compensate
        Assert.Contains(context.Events, e => 
            e.StepId == "Step2" && e.EventType == "compensation_skipped");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStoreDataInContext()
    {
        // Arrange
        var context = new SagaContext { SagaName = "TestSaga" };
        var step = new DataStoringSagaStep("DataStep");

        // Act
        var result = await _orchestrator.ExecuteAsync(context, new[] { step });

        // Assert
        Assert.True(result.Success);
        Assert.True(context.TryGet<string>("test_data", out var data));
        Assert.Equal("test_value", data);
    }
}

// Test helper classes
public class TestSagaStep : ISagaStep
{
    public string StepId { get; }
    public string Name { get; }
    public bool CanCompensate { get; set; } = true;
    public bool ShouldFail { get; set; } = false;
    public bool WasExecuted { get; private set; }
    public bool WasCompensated { get; private set; }

    public TestSagaStep(string id)
    {
        StepId = id;
        Name = id;
    }

    public Task ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        WasExecuted = true;
        
        if (ShouldFail)
        {
            throw new InvalidOperationException($"Step {StepId} failed intentionally");
        }

        return Task.CompletedTask;
    }

    public Task CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        WasCompensated = true;
        return Task.CompletedTask;
    }
}

public class DataStoringSagaStep : ISagaStep
{
    public string StepId { get; }
    public string Name { get; }
    public bool CanCompensate => true;

    public DataStoringSagaStep(string id)
    {
        StepId = id;
        Name = id;
    }

    public Task ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        context.Set("test_data", "test_value");
        return Task.CompletedTask;
    }

    public Task CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
