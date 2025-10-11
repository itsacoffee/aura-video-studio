# Aura CLI

## Overview

The Aura CLI is a cross-platform command-line tool for headless video generation and automation. It runs on Linux, macOS, and Windows, enabling automated video creation workflows without requiring the UI.

## Purpose

1. **Headless Generation**: Create videos programmatically via CLI commands
2. **CI/CD Integration**: Automate video generation in CI/CD pipelines
3. **Cross-Platform Testing**: Validate backend functionality on all platforms
4. **Development Tool**: Quick testing of provider implementations
5. **Documentation**: Living example of the video generation pipeline

## Commands

### `preflight`

Check system requirements and dependencies before generation.

```bash
aura-cli preflight [options]

Options:
  -v, --verbose    Show detailed system information
```

**Example:**
```bash
aura-cli preflight -v
```

**Output:**
- Hardware tier (A/B/C/D)
- CPU, RAM, GPU specifications
- FFmpeg availability
- Provider availability (TTS, Stable Diffusion, etc.)

### `script`

Generate a video script from brief and plan JSON files.

```bash
aura-cli script [options]

Options:
  -b, --brief <file>     Path to brief JSON file (required)
  -p, --plan <file>      Path to plan JSON file (required)
  -o, --output <file>    Output file path (default: script.txt)
  -v, --verbose          Enable verbose output
  --dry-run              Validate inputs without generating
```

**Example:**
```bash
aura-cli script -b brief.json -p plan.json -o script.txt
```

**Brief JSON Format:**
```json
{
  "topic": "Machine Learning Basics",
  "audience": "Beginners",
  "goal": "Understand ML fundamentals",
  "tone": "Educational",
  "language": "en-US",
  "aspect": "Widescreen16x9"
}
```

**Plan JSON Format:**
```json
{
  "targetDuration": "00:03:00",
  "pacing": "Conversational",
  "density": "Balanced",
  "style": "Educational"
}
```

### `quick`

Quick end-to-end generation with sensible defaults. This is the fastest way to generate video content.

```bash
aura-cli quick [options]

Options:
  -t, --topic <text>     Video topic (required)
  -d, --duration <mins>  Target duration in minutes (default: 3)
  -o, --output <dir>     Output directory (default: ./output)
  -v, --verbose          Enable verbose output
  --dry-run              Validate without generating files
```

**Examples:**
```bash
# Quick generation with defaults
aura-cli quick -t "Introduction to Coffee Brewing"

# Custom duration and output directory
aura-cli quick -t "Machine Learning" -d 5 -o ./videos

# Dry run to validate
aura-cli quick -t "Test Topic" --dry-run -v
```

**Generated Files:**
- `brief.json` - Video brief configuration
- `plan.json` - Timeline plan specification
- `script.txt` - Generated narration script

### `help`

Show help information.

```bash
aura-cli help
aura-cli <command> --help
```

## Features Demonstrated

### 1. Hardware Detection
- Detects CPU cores (logical and physical)
- Detects RAM capacity
- Detects GPU vendor, model, and VRAM
- Assigns hardware tier (A/B/C/D) based on VRAM
- Determines available encoders (NVENC/AMF/QSV/x264)
- Checks for Stable Diffusion support (NVIDIA-only)

**Graceful Fallback**: On non-Windows platforms, uses sensible defaults when WMI is unavailable.

### 2. Script Generation
- Uses `RuleBasedLlmProvider` (no API keys required)
- Generates multi-scene scripts based on:
  - Topic and audience
  - Target duration
  - Pacing (Chill, Conversational, Fast)
  - Content density (Minimal, Balanced, Dense)
- Demonstrates deterministic template system

### 3. Provider Mixing Explanation
- Shows three modes:
  - **Free Mode**: No API keys, uses rule-based LLM + Windows TTS + Stock visuals
  - **Balanced Mix**: Prefers Pro providers, falls back to Free
  - **Pro-Max**: Requires API keys for all stages

### 4. Acceptance Criteria Validation
- Checks all 9 acceptance criteria from spec
- Reports status for each criterion
- Highlights areas needing Windows for full testing

## Running the CLI

### Prerequisites
- .NET 8 SDK
- (Optional) FFmpeg installed for actual rendering

### Usage

**Show all commands:**
```bash
aura-cli help
```

**Quick generation (most common):**
```bash
aura-cli quick -t "Your Topic Here"
```

**Check system requirements:**
```bash
aura-cli preflight -v
```

**Legacy demo mode:**
```bash
aura-cli --demo
```

### CI/CD Integration

**GitHub Actions Example:**
```yaml
- name: Generate Video Script
  run: |
    dotnet run --project Aura.Cli -- quick -t "Automated Video" -o ./artifacts
```

**GitLab CI Example:**
```yaml
generate_video:
  script:
    - dotnet run --project Aura.Cli -- quick -t "$VIDEO_TOPIC" -o ./output
  artifacts:
    paths:
      - output/
```

### Expected Output

```
╔══════════════════════════════════════════════════════════╗
║           AURA VIDEO STUDIO - CLI Demo                  ║
║   Free-Path Video Generation (No API Keys Required)     ║
╚══════════════════════════════════════════════════════════╝

📊 Step 1: Hardware Detection
═══════════════════════════════════════════════════════════
  CPU: 16 logical cores (8 physical)
  RAM: 32 GB
  GPU: NVIDIA RTX 3080
  VRAM: 10 GB
  Hardware Tier: B
  NVENC Available: True
  SD Available: True (NVIDIA-only)
  Offline Mode: False

✍️  Step 2: Script Generation (Rule-Based LLM)
═══════════════════════════════════════════════════════════
  Topic: Introduction to Machine Learning
  Target Duration: 3 minutes
  Pacing: Conversational

  ✅ Generated script (2943 characters)
     Preview:
       # Introduction to Machine Learning
       
       ## Introduction
       Introduction to Machine Learning has become increasingly important...

[... additional output ...]

✅ Demo completed successfully!
```

## Architecture

### Program.cs
- Configures dependency injection
- Sets up logging (console output)
- Builds host with all services
- Runs the demo orchestration

### CliDemo Class
- Orchestrates demonstration steps
- Calls hardware detection
- Generates sample script
- Explains provider mixing
- Validates acceptance criteria
- Formats output with Unicode box-drawing characters

### Dependencies
- `Aura.Core` - Models, hardware detection, orchestration
- `Aura.Providers` - LLM, TTS, video composition providers
- `Microsoft.Extensions.*` - DI, hosting, logging

## Use Cases

### 1. CI/CD Pipeline
```yaml
# .github/workflows/ci.yml
- name: Run Backend Demo
  run: dotnet run --project Aura.Cli/Aura.Cli.csproj
```

Validates backend functionality on every push without Windows runner.

### 2. Quick Feature Testing
```bash
# Test a new provider implementation
dotnet run --project Aura.Cli/Aura.Cli.csproj
```

See immediate results without UI compilation.

### 3. Documentation
The demo output serves as living documentation showing:
- What the free path includes
- How hardware detection works
- Which providers are available
- How mixing and fallback operate

### 4. Debugging
Add breakpoints in CliDemo to inspect:
- Hardware detection results
- Script generation logic
- Provider selection decisions

## Extending the Demo

### Add New Demonstration Step

```csharp
// In CliDemo.RunAsync()

Console.WriteLine("🆕 Step 7: New Feature");
Console.WriteLine("═══════════════════════════════════════════════════════════");

// Your demonstration code here
var result = await _newService.DoSomethingAsync();
Console.WriteLine($"  ✅ Result: {result}");
Console.WriteLine();
```

### Test Different Scenarios

```csharp
// Test with different configurations
var brief = new Brief(
    Topic: "Your Topic Here",
    Audience: "Your Audience",
    Goal: "Your Goal",
    Tone: "Professional",
    Language: "en-US",
    Aspect: Aspect.Vertical9x16  // Test Shorts format
);

var planSpec = new PlanSpec(
    TargetDuration: TimeSpan.FromSeconds(60),  // 1-minute video
    Pacing: Pacing.Fast,
    Density: Density.Dense,
    Style: "Quick Tips"
);
```

### Add Provider Testing

```csharp
// Test different providers
services.AddTransient<ILlmProvider>(sp => 
    new OllamaLlmProvider(
        sp.GetRequiredService<ILogger<OllamaLlmProvider>>(),
        "http://localhost:11434"
    )
);
```

## Platform Considerations

### Windows
- Full hardware detection via WMI
- NVIDIA driver detection via nvidia-smi
- FFmpeg encoder detection
- Windows TTS available

### Linux/macOS
- Fallback hardware values used
- GPU detection limited
- Windows TTS unavailable (expected)
- FFmpeg detection works if installed

## Troubleshooting

### Issue: "Failed to get CPU info from WMI"
**Cause**: Running on non-Windows platform  
**Solution**: This is expected. Demo uses fallback values.

### Issue: "GPU: Not detected"
**Cause**: No WMI on Linux/macOS  
**Solution**: Expected behavior. Shows how tier D (no GPU) works.

### Issue: Build fails
**Cause**: Missing .NET 8 SDK  
**Solution**: Install from https://dot.net

## Output Formatting

The demo uses Unicode box-drawing characters for visual appeal:
- `╔══╗` for headers
- `═══` for section separators
- `📊 🎬 🎨` emoji for section icons
- `✅ ⚠️` for status indicators

To disable Unicode (e.g., for CI logs):
```csharp
// In Program.cs, set:
Console.OutputEncoding = System.Text.Encoding.ASCII;
```

## Integration with Main App

The CLI demo shares the same backend as the WinUI 3 app:
- Same models (`Aura.Core.Models`)
- Same providers (`Aura.Providers.*`)
- Same hardware detection (`HardwareDetector`)
- Same business logic

This ensures:
- ✅ Backend changes tested immediately
- ✅ No UI-specific coupling
- ✅ Cross-platform compatibility
- ✅ Faster development iteration

## Performance

Typical demo execution time:
- Hardware detection: < 1 second
- Script generation: < 100ms
- Total runtime: ~2-3 seconds

Memory usage:
- Startup: ~50 MB
- Peak: ~80 MB
- Cleanup: < 10 MB

## Comparison with WinUI App

| Feature | CLI Demo | WinUI App |
|---------|----------|-----------|
| Platform | Cross-platform | Windows only |
| UI | Console text | Rich XAML |
| Hardware Detection | ✅ | ✅ |
| Script Generation | ✅ | ✅ |
| TTS Synthesis | ⚠️ Limited | ✅ Full |
| Video Rendering | ⚠️ Simulated | ✅ Full |
| Interactive Editing | ❌ | ✅ Timeline |
| API Key Management | ❌ | ✅ Encrypted |
| First-run Wizard | ❌ | ✅ Dialog |

## Contributing

When adding new features to the backend:

1. **Add to CLI Demo**: Demonstrate the new feature
2. **Update Output**: Show feature status in acceptance criteria
3. **Document**: Explain what the feature does
4. **Test**: Ensure demo runs on all platforms

This ensures all features are:
- Testable without UI
- Validated on Linux CI
- Documented with working examples

## License

Same as main Aura Video Studio project.
