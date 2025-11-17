# Load Testing and Profiling Guide

## Overview

This guide provides a comprehensive plan for load testing and profiling Aura Video Studio to ensure it can handle large jobs without degradation, especially on Windows.

## Objectives

1. **Identify Performance Bottlenecks**: Find hot spots in both .NET backend and Node/Electron processes
2. **Memory Leak Detection**: Ensure memory usage remains stable during long-running operations
3. **Concurrent Job Handling**: Verify the system can handle multiple video generation jobs
4. **Windows-Specific Testing**: Validate performance on Windows with various hardware configurations
5. **Resource Usage Validation**: Confirm CPU, GPU, and memory usage are within acceptable limits

## Load Testing Scenarios

### Scenario 1: Sequential Video Generation

**Objective**: Test system stability when generating multiple videos one after another.

**Test Plan**:
1. Generate 5 videos sequentially (each 1-2 minutes long)
2. Monitor:
   - Memory usage after each video
   - CPU utilization
   - Disk I/O
   - Backend process memory
   - Electron process memory
3. **Success Criteria**:
   - Memory growth < 20% over 5 videos
   - No memory leaks (memory returns to baseline after GC)
   - CPU usage returns to idle between jobs
   - No crashes or errors

**Implementation**:
```bash
# Use the API to generate videos sequentially
for i in {1..5}; do
  curl -X POST http://localhost:5000/api/jobs \
    -H "Content-Type: application/json" \
    -d '{"projectId": "test-project", "duration": 120}'
  # Wait for job to complete
  sleep 60
done
```

### Scenario 2: Concurrent Video Generation

**Objective**: Test system behavior when multiple videos are generated simultaneously.

**Test Plan**:
1. Start 3 video generation jobs concurrently
2. Monitor:
   - Resource contention
   - Job completion times
   - Error rates
   - Memory usage per job
3. **Success Criteria**:
   - All jobs complete successfully
   - No significant performance degradation
   - Memory usage scales linearly (not exponentially)
   - No deadlocks or race conditions

**Implementation**:
```bash
# Start 3 concurrent jobs
for i in {1..3}; do
  curl -X POST http://localhost:5000/api/jobs \
    -H "Content-Type: application/json" \
    -d "{\"projectId\": \"test-project-$i\", \"duration\": 120}" &
done
wait
```

### Scenario 3: Long-Running Job

**Objective**: Test system stability during a single long-running job (10+ minutes).

**Test Plan**:
1. Generate a single 10-minute video
2. Monitor continuously:
   - Memory usage over time
   - CPU/GPU utilization
   - FFmpeg process health
   - Backend responsiveness
3. **Success Criteria**:
   - Memory usage remains stable (no continuous growth)
   - No crashes or timeouts
   - Backend remains responsive to health checks
   - FFmpeg processes don't accumulate

**Implementation**:
```bash
# Generate a long video
curl -X POST http://localhost:5000/api/jobs \
  -H "Content-Type: application/json" \
  -d '{"projectId": "long-test", "duration": 600}'
```

### Scenario 4: Stress Test - Maximum Concurrent Jobs

**Objective**: Find the maximum number of concurrent jobs the system can handle.

**Test Plan**:
1. Gradually increase concurrent jobs (1, 2, 3, 4, 5...)
2. Monitor system behavior at each level
3. Identify the breaking point
4. **Success Criteria**:
   - System gracefully handles maximum load
   - Clear error messages when limits are exceeded
   - No data corruption
   - System recovers after load decreases

## Profiling Tools and Setup

### .NET Backend Profiling

#### 1. **dotnet-counters** (Real-time Metrics)

**Setup**:
```bash
# Install dotnet-counters
dotnet tool install -g dotnet-counters

# Monitor the backend process
dotnet-counters monitor --process-id <PID> \
  System.Runtime \
  Microsoft.AspNetCore.Hosting
```

**Key Metrics to Monitor**:
- `gc-heap-size`: Managed memory usage
- `gen-0-gc-count`: Generation 0 GC frequency
- `gen-1-gc-count`: Generation 1 GC frequency
- `gen-2-gc-count`: Generation 2 GC frequency (indicates memory pressure)
- `exception-count`: Unhandled exceptions
- `threadpool-thread-count`: Thread pool usage
- `aspnet-requests-per-sec`: Request throughput

**Example Session**:
```bash
# Find the backend process ID
Get-Process -Name "Aura.Api" | Select-Object Id

# Monitor for 5 minutes
dotnet-counters collect --process-id <PID> \
  --format csv \
  --output performance.csv \
  --duration 00:05:00
```

#### 2. **dotnet-trace** (Event Tracing)

**Setup**:
```bash
# Install dotnet-trace
dotnet tool install -g dotnet-trace

# Collect trace during load test
dotnet-trace collect --process-id <PID> \
  --format speedscope \
  --output trace.nettrace
```

**Analyze with PerfView or Visual Studio**:
- Open `trace.nettrace` in Visual Studio
- View CPU sampling
- Identify hot methods
- Check allocation rates

#### 3. **dotnet-dump** (Memory Analysis)

**Setup**:
```bash
# Install dotnet-dump
dotnet tool install -g dotnet-dump

# Capture heap dump during high memory usage
dotnet-dump collect --process-id <PID> \
  --output memory.dump
```

**Analyze with dotnet-dump**:
```bash
# Analyze the dump
dotnet-dump analyze memory.dump

# Commands:
> dumpheap -stat          # Show heap statistics
> dumpheap -mt <MethodTable>  # Show objects of specific type
> gcroot <object-address>     # Find what's keeping object alive
```

#### 4. **Application Insights / Performance Profiler**

If Application Insights is configured:
- View live metrics dashboard
- Analyze performance traces
- Check dependency calls
- Review exception telemetry

### Electron/Node Profiling

#### 1. **Chrome DevTools Performance Profiler**

**Setup**:
1. Enable remote debugging in Electron:
   ```javascript
   // In main.js
   app.commandLine.appendSwitch('remote-debugging-port', '9222');
   ```

2. Open Chrome and navigate to `chrome://inspect`
3. Click "inspect" on the Electron process
4. Use Performance tab to record CPU profile

**Key Metrics**:
- Scripting time
- Rendering time
- Painting time
- Memory timeline

#### 2. **Node.js Built-in Profiler**

**Setup**:
```bash
# Start Electron with profiling
node --prof electron.js

# After test, generate report
node --prof-process isolate-*.log > profile.txt
```

#### 3. **clinic.js** (Comprehensive Node Profiling)

**Setup**:
```bash
# Install clinic.js
npm install -g clinic

# Profile Electron process
clinic doctor -- node electron.js

# Or use flame profiler
clinic flame -- node electron.js
```

#### 4. **Memory Profiling with Chrome DevTools**

**Setup**:
1. Open DevTools in Electron
2. Go to Memory tab
3. Take heap snapshots before and after operations
4. Compare snapshots to find memory leaks

**Analysis**:
- Look for objects that grow between snapshots
- Check for detached DOM nodes
- Identify event listeners that aren't cleaned up

### Windows-Specific Profiling

#### 1. **Windows Performance Recorder (WPR)**

**Setup**:
```powershell
# Start recording
wpr -start GeneralProfile -file trace.etl

# Run load test
# ... perform operations ...

# Stop recording
wpr -stop trace.etl
```

**Analyze with Windows Performance Analyzer (WPA)**:
- CPU usage by process/thread
- Memory allocations
- Disk I/O
- Network activity

#### 2. **Process Monitor (ProcMon)**

**Setup**:
1. Download Process Monitor from Microsoft
2. Filter for `Aura.Api.exe` and `electron.exe`
3. Monitor:
   - File system operations
   - Registry access
   - Network activity
   - Process/thread activity

#### 3. **Resource Monitor (resmon.exe)**

**Setup**:
1. Open Resource Monitor
2. Select Aura processes
3. Monitor:
   - CPU usage per thread
   - Memory usage (working set, private bytes)
   - Disk I/O (read/write bytes per second)
   - Network activity

## Memory Leak Detection

### Backend Memory Leak Detection

**Signs of Memory Leaks**:
1. Continuous growth in `gc-heap-size` even after operations complete
2. Increasing frequency of Gen-2 GCs
3. Objects not being collected after operations

**Detection Steps**:
1. **Baseline**: Take heap dump before load test
2. **During Test**: Monitor `gc-heap-size` continuously
3. **After Test**: Take another heap dump
4. **Compare**: Use dotnet-dump or PerfView to compare dumps

**Common Leak Sources**:
- Event handlers not unsubscribed
- Static collections growing
- Cached objects never evicted
- DbContext instances not disposed
- HttpClient instances not reused

### Frontend Memory Leak Detection

**Signs of Memory Leak**:
1. Memory usage grows continuously during normal operation
2. Objects persist in heap snapshots after component unmount
3. Event listeners accumulate

**Detection Steps**:
1. Take heap snapshot before operation
2. Perform operation multiple times
3. Take heap snapshot after operations
4. Compare snapshots in Chrome DevTools

**Common Leak Sources**:
- Event listeners not removed
- Timers/intervals not cleared
- SSE connections not closed
- Subscriptions not unsubscribed
- DOM references held after unmount

## Performance Benchmarks

### Target Metrics

| Metric | Target | Measurement Method |
|--------|--------|-------------------|
| Memory Growth (1 hour) | < 20% | dotnet-counters / Chrome DevTools |
| Gen-2 GC Frequency | < 1 per minute | dotnet-counters |
| P95 Request Latency | < 2s | Application logs / APM |
| Concurrent Jobs | ≥ 3 | Load test |
| FFmpeg Process Cleanup | 100% | Process monitoring |
| Electron Memory (idle) | < 500MB | Task Manager / Resource Monitor |
| Backend Memory (idle) | < 200MB | Task Manager / Resource Monitor |

## Load Testing Scripts

### PowerShell Load Test Script

```powershell
# load-test.ps1
param(
    [int]$ConcurrentJobs = 3,
    [int]$JobDuration = 120,
    [string]$ApiUrl = "http://localhost:5000"
)

$jobs = @()

Write-Host "Starting $ConcurrentJobs concurrent jobs..."

for ($i = 1; $i -le $ConcurrentJobs; $i++) {
    $job = Start-Job -ScriptBlock {
        param($url, $duration, $jobId)
        
        $body = @{
            projectId = "load-test-$jobId"
            duration = $duration
        } | ConvertTo-Json
        
        try {
            $response = Invoke-RestMethod -Uri "$url/api/jobs" `
                -Method POST `
                -ContentType "application/json" `
                -Body $body
            
            Write-Output "Job $jobId started: $($response.jobId)"
            return $response.jobId
        }
        catch {
            Write-Error "Job $jobId failed: $_"
            return $null
        }
    } -ArgumentList $ApiUrl, $JobDuration, $i
    
    $jobs += $job
}

Write-Host "Waiting for jobs to complete..."
$results = $jobs | Wait-Job | Receive-Job

Write-Host "Results:"
$results | ForEach-Object { Write-Host "  $_" }
```

### Python Load Test Script

```python
# load_test.py
import requests
import concurrent.futures
import time
import sys

API_URL = "http://localhost:5000"
CONCURRENT_JOBS = 3
JOB_DURATION = 120

def create_job(job_id):
    """Create a video generation job"""
    url = f"{API_URL}/api/jobs"
    payload = {
        "projectId": f"load-test-{job_id}",
        "duration": JOB_DURATION
    }
    
    try:
        response = requests.post(url, json=payload, timeout=30)
        response.raise_for_status()
        return response.json()
    except Exception as e:
        print(f"Job {job_id} failed: {e}")
        return None

def main():
    print(f"Starting {CONCURRENT_JOBS} concurrent jobs...")
    start_time = time.time()
    
    with concurrent.futures.ThreadPoolExecutor(max_workers=CONCURRENT_JOBS) as executor:
        futures = {
            executor.submit(create_job, i): i 
            for i in range(1, CONCURRENT_JOBS + 1)
        }
        
        results = []
        for future in concurrent.futures.as_completed(futures):
            job_id = futures[future]
            try:
                result = future.result()
                results.append(result)
                print(f"Job {job_id} completed: {result}")
            except Exception as e:
                print(f"Job {job_id} exception: {e}")
    
    elapsed = time.time() - start_time
    print(f"\nCompleted {len(results)} jobs in {elapsed:.2f} seconds")
    print(f"Average time per job: {elapsed / CONCURRENT_JOBS:.2f} seconds")

if __name__ == "__main__":
    main()
```

## Continuous Monitoring

### During Development

1. **Enable Performance Monitoring**:
   - Backend: Already enabled via `QueryPerformanceMiddleware`
   - Frontend: Enable via `localStorage.setItem('enablePerformanceMonitoring', 'true')`

2. **Monitor Logs**:
   - Backend: `AURA_LOGS_PATH/performance-*.log`
   - Electron: `%APPDATA%/Aura Video Studio/logs/`

3. **Use Performance Dashboard**:
   - Frontend: Navigate to `/performance` (if available)
   - Backend: `/api/performance/metrics`

### In Production

1. **Application Insights** (if configured):
   - View live metrics
   - Set up alerts for performance degradation
   - Analyze trends over time

2. **Log Aggregation**:
   - Collect logs from all instances
   - Analyze error rates
   - Monitor response times

3. **Health Checks**:
   - `/health` endpoint for overall health
   - `/health/ready` for readiness
   - Monitor these endpoints

## Troubleshooting Performance Issues

### High Memory Usage

1. **Check for Leaks**:
   - Take heap dumps before/after operations
   - Compare object counts
   - Look for growing collections

2. **Review GC Behavior**:
   - Monitor Gen-2 GC frequency
   - Check if GC is keeping up with allocations
   - Consider GC settings tuning

3. **Check Resource Disposal**:
   - Ensure `IDisposable` is implemented correctly
   - Verify `using` statements or `Dispose()` calls
   - Check for event handler leaks

### Slow Performance

1. **Profile CPU Usage**:
   - Use dotnet-trace or Chrome DevTools
   - Identify hot methods
   - Optimize frequently called code paths

2. **Check Database Queries**:
   - Review query performance logs
   - Check for N+1 queries
   - Verify indexes are used

3. **Review Network Calls**:
   - Check for unnecessary API calls
   - Verify caching is working
   - Look for slow external dependencies

### High CPU Usage

1. **Identify CPU-Intensive Operations**:
   - Profile with dotnet-trace
   - Check for tight loops
   - Look for blocking operations

2. **Optimize Algorithms**:
   - Use more efficient data structures
   - Parallelize where possible
   - Cache expensive computations

3. **Review Threading**:
   - Check thread pool usage
   - Look for thread contention
   - Verify async/await usage

## Best Practices

1. **Run Load Tests Regularly**: After major changes, run load tests to catch regressions
2. **Profile Before Optimizing**: Don't guess - profile first to find real bottlenecks
3. **Monitor in Production**: Set up continuous monitoring to catch issues early
4. **Document Baselines**: Keep records of baseline performance for comparison
5. **Automate Testing**: Integrate load tests into CI/CD pipeline where possible

## Resources

- [.NET Performance Tools](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/)
- [Chrome DevTools Performance](https://developer.chrome.com/docs/devtools/performance/)
- [Windows Performance Toolkit](https://docs.microsoft.com/en-us/windows-hardware/test/wpt/)
- [Node.js Profiling Guide](https://nodejs.org/en/docs/guides/simple-profiling/)

