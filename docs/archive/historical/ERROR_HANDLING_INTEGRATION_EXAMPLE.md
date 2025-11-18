# Error Handling Integration Example

This document demonstrates how all error handling components work together in a complete workflow.

## Scenario: Video Rendering with Multiple Failure Points

This example shows how the system handles a video rendering operation that encounters multiple errors and gracefully degrades to ensure the user gets a result.

### Backend Implementation

```csharp
using Aura.Core.Services.ErrorHandling;
using Aura.Core.Errors;

public class VideoRenderingService
{
    private readonly ILogger<VideoRenderingService> _logger;
    private readonly ErrorLoggingService _errorLoggingService;
    private readonly GracefulDegradationService _degradationService;
    private readonly ErrorRecoveryService _errorRecoveryService;
    private readonly IFFmpegService _ffmpegService;
    private readonly INotificationService _notificationService;

    public async Task<RenderResult> RenderVideoAsync(RenderRequest request)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        
        // Define fallback strategies in order of preference
        var fallbackStrategies = new List<FallbackStrategy<RenderResult>>
        {
            // Strategy 1: GPU fails -> Try CPU rendering
            _degradationService.CreateGpuToCpuFallback(
                () => RenderWithCpuAsync(request),
                "GPU rendering failed. Using CPU rendering (slower but reliable)."),
            
            // Strategy 2: CPU rendering too slow -> Low quality mode
            _degradationService.CreateLowQualityFallback(
                () => RenderLowQualityAsync(request),
                "Rendering in low quality mode to save resources."),
            
            // Strategy 3: Complete failure -> Save partial results
            _degradationService.CreatePartialSaveFallback(
                async (ex) => await SavePartialResultAsync(request, ex),
                "Saving partial results. Some content may be incomplete.")
        };

        try
        {
            _logger.LogInformation(
                "Starting video render: JobId={JobId}, Quality={Quality}, GPU={UseGpu} [CorrelationId: {CorrelationId}]",
                request.JobId,
                request.Quality,
                request.UseGpuAcceleration,
                correlationId);

            // Execute with graceful degradation
            var result = await _degradationService.ExecuteWithFallbackAsync(
                () => RenderWithGpuAsync(request),
                fallbackStrategies,
                "VideoRendering",
                correlationId);

            if (result.Success)
            {
                // Log success
                await _errorLoggingService.LogErrorAsync(
                    new Exception($"Render completed successfully with strategy: {result.FallbackStrategy ?? "Primary"}"),
                    ErrorCategory.Application,
                    correlationId,
                    context: new Dictionary<string, object>
                    {
                        ["jobId"] = request.JobId,
                        ["usedFallback"] = result.UsedFallback,
                        ["qualityDegradation"] = result.QualityDegradation?.ToString() ?? "None",
                        ["attemptHistory"] = result.AttemptHistory
                    });

                // Notify user if we used a fallback
                if (result.UsedFallback && result.UserNotification != null)
                {
                    await _notificationService.NotifyWarningAsync(
                        "Video Rendered with Fallback",
                        result.UserNotification,
                        correlationId);
                }

                return result.Result!;
            }
            else
            {
                // All strategies failed - log and throw
                await _errorLoggingService.LogErrorAsync(
                    result.Error!,
                    ErrorCategory.System,
                    correlationId,
                    context: new Dictionary<string, object>
                    {
                        ["jobId"] = request.JobId,
                        ["attemptHistory"] = result.AttemptHistory
                    },
                    writeImmediately: true);

                // Generate recovery guide for the user
                var guide = _errorRecoveryService.GenerateRecoveryGuide(result.Error!, correlationId);

                // Try automated recovery one last time
                if (guide.AutomatedRecovery != null)
                {
                    var recoveryResult = await _errorRecoveryService.AttemptAutomatedRecoveryAsync(
                        result.Error!,
                        correlationId);

                    if (recoveryResult.Success)
                    {
                        // Retry after recovery
                        _logger.LogInformation(
                            "Automated recovery successful, retrying render [CorrelationId: {CorrelationId}]",
                            correlationId);
                        return await RenderVideoAsync(request); // Recursive retry
                    }
                }

                // Create user-friendly exception with recovery guide
                throw new RenderException(
                    "Video rendering failed after all fallback attempts",
                    RenderErrorCategory.ProcessFailed,
                    jobId: request.JobId,
                    correlationId: correlationId,
                    suggestedActions: guide.ManualActions.ToArray())
                    .WithContext("recoveryGuide", guide)
                    .WithContext("attemptHistory", result.AttemptHistory);
            }
        }
        catch (Exception ex) when (ex is not RenderException)
        {
            // Unexpected error - log and wrap
            await _errorLoggingService.LogErrorAsync(
                ex,
                ErrorCategory.Application,
                correlationId,
                context: new Dictionary<string, object>
                {
                    ["jobId"] = request.JobId,
                    ["operation"] = "RenderVideo"
                },
                writeImmediately: true);

            throw;
        }
    }

    private async Task<RenderResult> RenderWithGpuAsync(RenderRequest request)
    {
        // Attempt GPU-accelerated rendering
        if (!_hardwareDetector.HasNvidiaGpu())
        {
            throw new RenderException(
                "NVIDIA GPU not available",
                RenderErrorCategory.HardwareEncoderFailed);
        }

        return await _ffmpegService.RenderAsync(request with { UseGpuAcceleration = true });
    }

    private async Task<RenderResult> RenderWithCpuAsync(RenderRequest request)
    {
        // Fallback to CPU rendering
        return await _ffmpegService.RenderAsync(request with { UseGpuAcceleration = false });
    }

    private async Task<RenderResult> RenderLowQualityAsync(RenderRequest request)
    {
        // Render at lower quality
        return await _ffmpegService.RenderAsync(request with 
        { 
            UseGpuAcceleration = false,
            Quality = RenderQuality.Low,
            Resolution = "720p"
        });
    }

    private async Task<RenderResult> SavePartialResultAsync(RenderRequest request, Exception error)
    {
        // Save whatever was completed successfully
        return await _ffmpegService.SavePartialAsync(request, error);
    }
}
```

### Frontend Implementation

```typescript
import { useState } from 'react';
import { useErrorHandler } from '../hooks/useErrorHandler';
import { ErrorDialog } from '../components/Errors/ErrorDialog';
import { useNotifications } from '../components/Notifications/Toasts';
import { exportDiagnostics } from '../api/diagnosticsClient';

interface RenderPageProps {
  jobId: string;
}

export function RenderPage({ jobId }: RenderPageProps) {
  const [isRendering, setIsRendering] = useState(false);
  const { currentError, showErrorDialog, handleError, clearError, retryOperation } = useErrorHandler();
  const { showSuccessToast, showFailureToast } = useNotifications();

  const handleRender = async () => {
    setIsRendering(true);
    try {
      const result = await fetch(`/api/render/${jobId}`, {
        method: 'POST',
      });

      if (!result.ok) {
        const errorData = await result.json();
        throw new Error(errorData.message || 'Render failed');
      }

      const data = await result.json();

      // Check if fallback was used
      if (data.usedFallback) {
        showSuccessToast({
          title: 'Video Rendered (Fallback Used)',
          message: data.userNotification || 'Video rendered using alternative method',
          outputPath: data.outputPath,
          duration: data.duration,
          onOpenFile: () => window.electron.openFile(data.outputPath),
          onOpenFolder: () => window.electron.openFolder(data.outputPath),
        });
      } else {
        showSuccessToast({
          title: 'Video Rendered Successfully',
          message: 'Your video has been rendered',
          outputPath: data.outputPath,
          duration: data.duration,
          onOpenFile: () => window.electron.openFile(data.outputPath),
          onOpenFolder: () => window.electron.openFolder(data.outputPath),
        });
      }
    } catch (error) {
      // Handle error with full error handling service
      await handleError(error as Error, {
        operation: 'render',
        jobId,
        timestamp: new Date().toISOString(),
      }, {
        reportToBackend: true,
        attemptRecovery: true,
        maxRetries: 3,
        showDialog: true,
      });

      showFailureToast({
        title: 'Render Failed',
        message: (error as Error).message,
        onOpenLogs: () => {
          // Open logs page
          window.location.href = '/diagnostics/logs';
        },
      });
    } finally {
      setIsRendering(false);
    }
  };

  const handleExportDiagnostics = async () => {
    try {
      const blob = await exportDiagnostics(24); // Last 24 hours
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `diagnostics-${Date.now()}.json`;
      a.click();
      URL.revokeObjectURL(url);
    } catch (error) {
      console.error('Failed to export diagnostics:', error);
    }
  };

  return (
    <div>
      <h1>Render Video</h1>
      
      <button 
        onClick={handleRender} 
        disabled={isRendering}
      >
        {isRendering ? 'Rendering...' : 'Render Video'}
      </button>

      {/* Error Dialog with full recovery options */}
      {currentError && (
        <ErrorDialog
          open={showErrorDialog}
          onClose={clearError}
          error={currentError}
          onRetry={() => retryOperation(handleRender)}
          onExportDiagnostics={handleExportDiagnostics}
        />
      )}
    </div>
  );
}
```

### Frontend: App-Level Error Boundary

```typescript
import { ErrorBoundary } from './components/Errors/ErrorBoundary';
import { NotificationsToaster } from './components/Notifications/Toasts';

function App() {
  return (
    <ErrorBoundary
      onError={(error, errorInfo) => {
        // Log to console
        console.error('Uncaught error:', error, errorInfo);
        
        // Could also send to external service
        if (process.env.NODE_ENV === 'production') {
          // sendToSentry(error, errorInfo);
        }
      }}
    >
      <NotificationsToaster toasterId="notifications-toaster" />
      <Router>
        <Routes>
          <Route path="/render/:jobId" element={<RenderPage />} />
          {/* Other routes */}
        </Routes>
      </Router>
    </ErrorBoundary>
  );
}
```

## Complete Error Flow

1. **User initiates render** → Frontend calls API
2. **Backend attempts GPU rendering** → Fails (GPU not available)
3. **Graceful degradation** → Falls back to CPU rendering
4. **CPU rendering succeeds** → User notified about fallback
5. **Error logged** → Stored with correlation ID for troubleshooting
6. **User sees success toast** → With note about using CPU instead of GPU
7. **If all attempts failed** → Error dialog shows with:
   - User-friendly error message
   - Suggested actions
   - Troubleshooting steps
   - Documentation links
   - Option to retry
   - Option to export diagnostics

## Error Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    User Action (Render)                      │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              Primary: GPU Rendering Attempt                  │
└────────────────────────┬────────────────────────────────────┘
                         │
                    ┌────┴────┐
                    │  Fails  │
                    └────┬────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│         Fallback 1: CPU Rendering Attempt                    │
│         (Quality Degradation: Minor)                         │
└────────────────────────┬────────────────────────────────────┘
                         │
                    ┌────┴────┐
                    │  Fails  │
                    └────┬────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│         Fallback 2: Low Quality Rendering                    │
│         (Quality Degradation: Significant)                   │
└────────────────────────┬────────────────────────────────────┘
                         │
                    ┌────┴────┐
                    │  Fails  │
                    └────┬────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│         Fallback 3: Partial Save                             │
│         (Quality Degradation: Severe)                        │
└────────────────────────┬────────────────────────────────────┘
                         │
                    ┌────┴────┐
                    │  Fails  │
                    └────┬────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│         Error Logged + Recovery Guide Generated              │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│         Automated Recovery Attempt?                          │
└────────────────────────┬────────────────────────────────────┘
                         │
                    ┌────┴────┐
                    │ Success │
                    └────┬────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│         Retry Original Operation                             │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
                    ┌────────┐
                    │ Result │
                    └────────┘
```

## Key Benefits Demonstrated

1. **No User Interruption**: System tries multiple fallbacks before giving up
2. **Transparent Degradation**: User informed about quality tradeoffs
3. **Comprehensive Logging**: All attempts logged with correlation ID
4. **Recovery Options**: Automated and manual recovery paths
5. **Context-Sensitive Help**: Error messages include relevant documentation
6. **Diagnostic Export**: Full system state available for support
7. **Retry Logic**: Smart retry with exponential backoff
8. **User Notification**: Non-intrusive toasts for success with fallback notes

This integration ensures users always get the best possible result while maintaining visibility into any degradations or issues.
