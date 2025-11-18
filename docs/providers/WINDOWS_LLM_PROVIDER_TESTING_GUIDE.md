# Windows LLM Provider Testing Guide

## Quick Start

This guide provides step-by-step instructions for testing LLM provider integration on Windows.

---

## Prerequisites

### Required

- ✅ Windows 10 20H2 or later (Windows 11 recommended)
- ✅ .NET 8.0 SDK installed
- ✅ Visual Studio 2022 or VS Code with C# extension
- ✅ Git for Windows

### Optional (for specific tests)

- OpenAI API key (for OpenAI provider tests)
- Anthropic API key (for Anthropic provider tests)
- Google Gemini API key (for Gemini provider tests)
- Ollama installed and running (for Ollama provider tests)

---

## Setup Instructions

### 1. Clone Repository

```powershell
git clone <repository-url>
cd aura-video-studio
```

### 2. Install Dependencies

```powershell
# Restore NuGet packages
dotnet restore

# Build solution
dotnet build
```

### 3. Set Environment Variables (Optional)

For testing cloud LLM providers, set your API keys:

```powershell
# PowerShell
$env:OPENAI_API_KEY="sk-your-openai-key-here"
$env:ANTHROPIC_API_KEY="sk-ant-your-anthropic-key-here"
$env:GEMINI_API_KEY="your-gemini-key-here"

# To persist across sessions, set user environment variables
[Environment]::SetEnvironmentVariable("OPENAI_API_KEY", "sk-...", "User")
[Environment]::SetEnvironmentVariable("ANTHROPIC_API_KEY", "sk-ant-...", "User")
[Environment]::SetEnvironmentVariable("GEMINI_API_KEY", "...", "User")
```

**Note**: API keys are optional. Tests will be skipped if keys are not provided.

### 4. Install Ollama (Optional)

For testing local Ollama provider:

1. Download Ollama for Windows from https://ollama.ai/download
2. Install and run Ollama
3. Pull a test model:

```powershell
ollama pull llama3.1:8b-q4_k_m
```

---

## Running Tests

### Run All Windows Integration Tests

```powershell
# Navigate to test directory
cd Aura.Tests

# Run all Windows integration tests
dotnet test --filter "Category=Windows&Category=Integration"
```

### Run Specific Provider Tests

```powershell
# Test OpenAI provider
dotnet test --filter "Category=Windows&Category=OpenAI"

# Test Anthropic provider
dotnet test --filter "Category=Windows&Category=Anthropic"

# Test Gemini provider
dotnet test --filter "Category=Windows&Category=Gemini"

# Test Ollama provider
dotnet test --filter "Category=Windows&Category=Ollama"
```

### Run Tests with Detailed Output

```powershell
# Show detailed test output
dotnet test --filter "Category=Windows" --logger "console;verbosity=detailed"
```

---

## Test Scenarios

### 1. Windows Credential Manager Test

**Purpose**: Verify API key storage in Windows Credential Manager

**Test**: `WindowsCredentialManager_ShouldStoreAndRetrieveApiKeys`

**What it tests**:
- ✅ Storing API keys securely
- ✅ Retrieving stored API keys
- ✅ Checking if API keys exist
- ✅ Deleting API keys
- ✅ Error handling

**Expected Result**: All operations should succeed without errors

**Manual Verification**:
```powershell
# View stored credentials
cmdkey /list | findstr "AuraVideoStudio"

# Should show: Target: AuraVideoStudio_TestProvider
```

---

### 2. Ollama Detection Test

**Purpose**: Test local Ollama installation detection

**Test**: `OllamaDetection_ShouldDetectLocalInstallation`

**Prerequisites**: Ollama must be running

**Start Ollama**:
```powershell
# Option 1: Run Ollama from Start Menu
# Option 2: Run from command line
ollama serve
```

**What it tests**:
- ✅ Service detection (HTTP health check)
- ✅ Version detection
- ✅ Model enumeration
- ✅ Model metadata retrieval

**Expected Output**:
```
Ollama Detection Results:
  IsRunning: True
  IsInstalled: True
  Version: 0.1.47
  BaseUrl: http://localhost:11434
  Error: None

  Available Models: 3
    - llama3.1:8b-q4_k_m (4.69 GB)
    - mistral:7b (4.11 GB)
    - codellama:7b (3.83 GB)
```

---

### 3. OpenAI Provider Test

**Purpose**: Test OpenAI API connectivity and error handling

**Test**: `OpenAI_ShouldHandleNetworkRequestsOnWindows`

**Prerequisites**: Set `OPENAI_API_KEY` environment variable

**What it tests**:
- ✅ API key validation
- ✅ Network request handling
- ✅ Model enumeration
- ✅ Error handling

**Expected Output**:
```
OpenAI API Key Validation:
  IsValid: True
  Message: API key is valid
  Available Models: 52
    - gpt-4o
    - gpt-4o-mini
    - gpt-4-turbo
    - gpt-3.5-turbo
    - ...
```

---

### 4. Network Error Handling Test

**Purpose**: Verify graceful error handling

**Test**: `NetworkFailure_ShouldHandleGracefully`

**What it tests**:
- ✅ Invalid API key handling
- ✅ User-friendly error messages
- ✅ No application crashes

**Expected Output**:
```
Network Error Handling Test:
  IsValid: False
  Message: Invalid API key
  ✓ Error handled gracefully without exception
```

---

### 5. Timeout Handling Test

**Purpose**: Verify timeout handling doesn't crash the application

**Test**: `Timeout_ShouldHandleGracefully`

**What it tests**:
- ✅ Request timeout handling
- ✅ No blocking calls
- ✅ Proper cancellation

**Expected Output**:
```
Timeout Handling Test:
  Service Available: False
  ✓ Timeout handled gracefully without exception
```

---

### 6. HTTP Client Proxy Test

**Purpose**: Verify Windows system proxy integration

**Test**: `WindowsHttpClient_ShouldUseSystemProxy`

**What it tests**:
- ✅ UseProxy configuration
- ✅ Default credentials support
- ✅ Automatic decompression

**Expected Output**:
```
HttpClient Windows Configuration:
  UseProxy: True
  UseDefaultCredentials: True
  Supports Automatic Decompression: True
  ✓ HttpClient configured to use Windows system proxy
```

---

## Troubleshooting

### Tests are Skipped

**Cause**: Missing prerequisites (API keys, Ollama, etc.)

**Solution**:
1. Check if environment variables are set
2. Verify Ollama is running (for Ollama tests)
3. Check test output for skip reasons

```powershell
# Verify environment variables
Get-ChildItem Env: | Where-Object { $_.Name -like "*API_KEY*" }
```

### Ollama Not Detected

**Symptom**: `IsRunning: False` in Ollama detection test

**Solutions**:

1. **Start Ollama service**:
```powershell
# Start Ollama
ollama serve

# Or run as Windows service
sc start ollama
```

2. **Check Ollama is listening**:
```powershell
Test-NetConnection -ComputerName localhost -Port 11434
```

3. **Check firewall**:
```powershell
# Check if port 11434 is blocked
Get-NetFirewallRule | Where-Object { $_.DisplayName -like "*Ollama*" }
```

### Network Connection Errors

**Symptom**: `HttpRequestException` or timeout errors

**Solutions**:

1. **Check internet connectivity**:
```powershell
Test-NetConnection api.openai.com -Port 443
Test-NetConnection api.anthropic.com -Port 443
```

2. **Check proxy settings**:
```powershell
# View proxy settings
netsh winhttp show proxy

# If behind corporate proxy, set:
netsh winhttp set proxy proxy-server="proxy.company.com:8080"
```

3. **Check Windows Defender Firewall**:
```powershell
# Check firewall status
Get-NetFirewallProfile | Select-Object Name, Enabled

# Temporarily disable (for testing only)
Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled False

# Re-enable after testing
Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled True
```

### Credential Manager Access Denied

**Symptom**: Access denied when storing/retrieving credentials

**Solutions**:

1. **Run as Administrator** (if required):
```powershell
# Right-click PowerShell → Run as Administrator
```

2. **Check Windows User Account Control (UAC)**:
- Ensure UAC is not blocking credential access

3. **Verify Windows login**:
- Make sure you're logged in with a valid Windows account

---

## Best Practices

### 1. API Key Security

❌ **Don't**:
- Commit API keys to source control
- Share API keys in plain text
- Store API keys in configuration files

✅ **Do**:
- Use environment variables
- Use Windows Credential Manager
- Use Azure Key Vault or similar for production

### 2. Testing on Corporate Networks

If testing behind a corporate proxy:

```powershell
# Configure system proxy
netsh winhttp set proxy proxy-server="proxy:8080" bypass-list="localhost"

# Test proxy configuration
netsh winhttp show proxy
```

### 3. Ollama Model Management

```powershell
# List available models
ollama list

# Pull specific model
ollama pull llama3.1:8b-q4_k_m

# Remove unused models to save space
ollama rm model-name

# Check Ollama version
ollama --version
```

---

## Continuous Integration

### GitHub Actions (Windows Runner)

```yaml
name: Windows LLM Provider Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Run Windows integration tests
      run: dotnet test --filter "Category=Windows&Category=Integration"
      env:
        OPENAI_API_KEY: ${{ secrets.OPENAI_API_KEY }}
        ANTHROPIC_API_KEY: ${{ secrets.ANTHROPIC_API_KEY }}
        GEMINI_API_KEY: ${{ secrets.GEMINI_API_KEY }}
```

---

## Additional Resources

### Documentation

- [Main Validation Report](./PR_CORE_001_LLM_PROVIDER_VALIDATION_REPORT.md)
- [Windows Credential Manager API](https://docs.microsoft.com/en-us/windows/win32/secauthn/authentication-functions)
- [Ollama Documentation](https://github.com/ollama/ollama/tree/main/docs)

### Support

For issues or questions:
1. Check the main validation report
2. Review test output for detailed error messages
3. Check Windows Event Viewer for system-level errors
4. Review application logs

---

## Summary Checklist

Before completing validation:

- [ ] All tests pass on Windows 10/11
- [ ] API key storage in Credential Manager verified
- [ ] At least one cloud provider (OpenAI/Anthropic/Gemini) tested
- [ ] Ollama detection tested (if applicable)
- [ ] Error handling verified
- [ ] Network timeout handling verified
- [ ] Proxy configuration verified (if applicable)
- [ ] Documentation reviewed

---

**Last Updated**: 2025-11-11  
**Version**: 1.0  
**Status**: Final
