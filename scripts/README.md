# Aura Video Studio - Test Scripts

This directory contains various scripts for testing and validating Aura Video Studio functionality.

## Pipeline Validation

### validate_pipeline.sh
**Purpose**: Comprehensive end-to-end validation of the complete video generation pipeline

**Requirements**:
- Backend API running (`dotnet run --project Aura.Api`)
- FFmpeg installed and in PATH
- At least one LLM provider configured
- At least one TTS provider configured

**Usage**:
```bash
./scripts/validate_pipeline.sh
```

**What it tests**:
1. Backend API health check
2. Frontend availability (optional)
3. Job creation via Quick Demo
4. Progress tracking through all pipeline stages
5. Script generation (LLM)
6. TTS audio generation
7. Image generation
8. FFmpeg video assembly
9. Video download and verification
10. Job cancellation
11. Temporary file cleanup
12. Log capture validation

**Output**:
- Test results summary to console
- Test artifacts in `./test-output/pipeline-validation/`
- Exit code 0 on success, 1 on failure

**Configuration**:
Environment variables:
- `API_BASE`: API endpoint (default: http://127.0.0.1:5000)
- `FRONTEND_BASE`: Frontend URL (default: http://127.0.0.1:5173)
- `TEST_OUTPUT_DIR`: Output directory (default: ./test-output/pipeline-validation)

## Quick Demo Validation

### run_quick_generate_demo.sh
**Purpose**: Fast smoke test that generates a demo video

**Usage**:
```bash
./scripts/run_quick_generate_demo.sh
```

**What it tests**:
- API health check
- Script generation endpoint
- Quick render endpoint
- Fallback to FFmpeg-only demo if API unavailable
- Caption file generation (SRT format)
- Log archive creation

**Output**:
- Demo video: `artifacts/smoke/demo.mp4`
- Captions: `artifacts/smoke/demo.srt`
- Logs: `artifacts/smoke/logs.zip`

## PowerShell Scripts

### run_e2e_local.ps1
**Purpose**: End-to-end test for local video generation using local engines

**Usage**:
```powershell
.\scripts\run_e2e_local.ps1
```

**Options**:
- `-EngineCheck`: Only check engine status without generating video
- `-SkipValidation`: Skip pre-flight validation checks
- `-OutputDir`: Directory for test output (default: ./test-output)

**What it tests**:
- API availability
- System capabilities detection
- Local engines status (Piper TTS, Stable Diffusion, Ollama)
- Video generation with local engines
- Output file validation

### run_quick_generate_demo.ps1
**Purpose**: Windows version of quick demo smoke test

**Usage**:
```powershell
.\scripts\run_quick_generate_demo.ps1
```

Similar functionality to the bash version but for Windows environments.

## Other Scripts

### check_coverage_thresholds.sh
**Purpose**: Validates code coverage meets project thresholds

**Usage**:
```bash
./scripts/check_coverage_thresholds.sh
```

## Running All Tests

To run a comprehensive test suite:

```bash
# 1. Start backend
cd Aura.Api
dotnet run &
API_PID=$!
cd ..

# Wait for API to be ready
sleep 5

# 2. Run unit tests
dotnet test Aura.E2E/Aura.E2E.csproj --configuration Release

# 3. Run pipeline validation
./scripts/validate_pipeline.sh

# 4. Run quick demo smoke test
./scripts/run_quick_generate_demo.sh

# 5. Cleanup
kill $API_PID
```

## CI/CD Integration

These scripts are integrated into the CI/CD pipeline:
- `validate_pipeline.sh` - Used in PR validation workflow
- `run_quick_generate_demo.sh` - Used in smoke test workflow
- `check_coverage_thresholds.sh` - Used in coverage workflow

## Troubleshooting

### Script Fails with "API not available"
**Solution**: Ensure backend is running:
```bash
cd Aura.Api
dotnet run
```

### Script Fails with "FFmpeg not found"
**Solution**: Install FFmpeg and ensure it's in PATH:
```bash
# Ubuntu/Debian
sudo apt-get install ffmpeg

# macOS
brew install ffmpeg

# Windows
choco install ffmpeg
```

### Script Hangs During Video Generation
**Solution**: Check:
1. Provider services are running (Ollama, etc.)
2. Network connectivity for cloud providers
3. System resources (CPU, memory, disk)
4. Logs in console output or Aura.Api/logs

### Permission Denied Error
**Solution**: Make script executable:
```bash
chmod +x scripts/*.sh
```

## Adding New Test Scripts

When adding new test scripts:

1. Place in appropriate subdirectory:
   - `scripts/smoke/` - Quick validation tests
   - `scripts/engines/` - Engine-specific tests
   - `scripts/cli/` - CLI tool tests
   - `scripts/` - General test scripts

2. Follow naming convention:
   - Bash scripts: `test_name.sh`
   - PowerShell scripts: `TestName.ps1`

3. Include header comment with:
   - Purpose
   - Requirements
   - Usage example
   - Environment variables

4. Make executable:
   ```bash
   chmod +x scripts/test_name.sh
   ```

5. Document in this README

6. Add to CI/CD pipeline if appropriate

## Support

For issues or questions about test scripts:
1. Check script output for error messages
2. Review logs in test output directories
3. Check GitHub Issues for known problems
4. Create new issue with error details
