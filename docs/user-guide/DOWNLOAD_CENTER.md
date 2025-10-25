# Download Center - Manifest-Driven Component Management

## Overview

The Download Center provides a comprehensive solution for managing dependencies and external tools required for Aura Video Studio. It implements a manifest-driven approach with robust verification, repair, and offline support capabilities.

## Features

### 1. Manifest-Driven Downloads

All components are defined in `manifest.json` with complete metadata:

```json
{
  "components": [
    {
      "name": "FFmpeg",
      "version": "6.0",
      "isRequired": true,
      "installPath": "dependencies/ffmpeg",
      "postInstallProbe": "ffmpeg",
      "files": [
        {
          "filename": "ffmpeg.exe",
          "url": "https://github.com/...",
          "sha256": "e25bfb9fc6986e5e42b0bcff64c20433...",
          "extractPath": "bin/ffmpeg.exe",
          "sizeBytes": 83558400
        }
      ]
    }
  ]
}
```

### 2. SHA-256 Verification

Every file is verified using SHA-256 checksums to ensure integrity:
- Downloads are verified immediately after completion
- Existing files are verified on status checks
- Corrupted files are automatically detected

### 3. Resume Support

Downloads can be interrupted and resumed automatically:
- Uses HTTP Range requests when supported by the server
- Falls back to full re-download if server doesn't support ranges
- Tracks partial downloads and continues from last byte

### 4. Repair Functionality

Detect and repair corrupted or incomplete installations:
- `VerifyComponentAsync`: Checks all files and their checksums
- `RepairComponentAsync`: Re-downloads only corrupted/missing files
- UI displays "Needs Repair" status automatically

### 5. Post-Install Validation

Components can specify validation probes:
- **FFmpeg**: Executes `ffmpeg -version` to verify installation
- **Ollama**: Checks if endpoint at `http://127.0.0.1:11434/api/tags` is reachable
- **Stable Diffusion**: Verifies WebUI endpoint at `http://127.0.0.1:7860/sdapi/v1/sd-models`

### 6. Offline Mode Support

For environments without internet access:
- Click "Manual" button for detailed installation instructions
- Shows download URLs, checksums, and install paths
- Users can verify files manually using provided SHA-256 checksums

### 7. Component Management

Complete lifecycle management:
- **Install**: Download and verify components
- **Verify**: Check integrity of installed components
- **Repair**: Fix corrupted or incomplete installations
- **Remove**: Delete component files
- **Open Folder**: Navigate to component directory

## API Endpoints

### Get Manifest
```
GET /api/downloads/manifest
```

### Check Component Status
```
GET /api/downloads/{component}/status
```

### Verify Component Integrity
```
GET /api/downloads/{component}/verify
```

### Install Component
```
POST /api/downloads/{component}/install
```

### Repair Component
```
POST /api/downloads/{component}/repair
```

### Remove Component
```
DELETE /api/downloads/{component}
```

### Get Component Folder Path
```
GET /api/downloads/{component}/folder
```

### Get Manual Installation Instructions
```
GET /api/downloads/{component}/manual
```

## User Interface

### Downloads Page

The Downloads page (`/downloads`) provides a centralized interface for managing all components:

#### Features:
1. **Component List**
   - Shows all available components with version info
   - Displays file sizes for each component
   - Indicates required vs. optional components

2. **Status Display**
   - ✅ **Installed**: Component is properly installed and verified
   - ⚠️ **Needs Repair**: Checksum verification failed
   - 🔴 **Not Installed**: Component is not present
   - ⏳ **Installing...**: Download in progress
   - 🔧 **Repairing...**: Repair operation in progress

3. **Actions**
   - **Install**: Download and install the component
   - **Manual**: Show offline installation instructions
   - **Repair**: Fix corrupted installation
   - **Open Folder**: View component files
   - **Remove**: Delete component

4. **Progress Tracking**
   - Real-time download progress (future enhancement)
   - Percentage complete
   - Transfer speed and time remaining (future enhancement)

5. **Error Messages**
   - Clear error reporting for failed operations
   - Retry options for network errors

## Usage Examples

### Installing a Component

1. Navigate to **Downloads** page
2. Find the component (e.g., "FFmpeg")
3. Click **Install** button
4. Wait for download and verification to complete
5. Status will show "Installed" with probe result

### Repairing a Corrupted Component

1. If status shows "Needs Repair"
2. Click **Repair** button
3. System will re-download corrupted/missing files
4. Verification will run automatically

### Manual Installation (Offline Mode)

1. Click **Manual** button for any component
2. Follow the displayed instructions:
   - Download files from provided URLs
   - Verify SHA-256 checksums
   - Place files in specified directories
3. Refresh status to verify manual installation

### Removing a Component

1. Click **Remove** button
2. Confirm deletion
3. All component files will be deleted
4. Status will update to "Not Installed"

## Implementation Details

### DependencyManager Class

Core class handling all download operations:

```csharp
public class DependencyManager
{
    // Load manifest from file or create default
    public Task<DependencyManifest> LoadManifestAsync()
    
    // Check if component is installed
    public Task<bool> IsComponentInstalledAsync(string componentName)
    
    // Verify component integrity
    public Task<ComponentVerificationResult> VerifyComponentAsync(string componentName)
    
    // Download/install component
    public Task DownloadComponentAsync(string componentName, IProgress<DownloadProgress> progress, CancellationToken ct)
    
    // Repair corrupted component
    public Task RepairComponentAsync(string componentName, IProgress<DownloadProgress> progress, CancellationToken ct)
    
    // Remove component
    public Task RemoveComponentAsync(string componentName)
    
    // Get manual installation instructions
    public ManualInstallInstructions GetManualInstallInstructions(string componentName)
}
```

### Resume Download Logic

```csharp
private async Task DownloadFileAsync(string url, string filePath, long expectedSize, ...)
{
    // Check for existing partial file
    long existingBytes = File.Exists(filePath) ? new FileInfo(filePath).Length : 0;
    
    // Create HTTP request with Range header
    var request = new HttpRequestMessage(HttpMethod.Get, url);
    if (existingBytes > 0)
    {
        request.Headers.Range = new RangeHeaderValue(existingBytes, null);
    }
    
    // Handle server response (PartialContent or full restart)
    // Append to existing file or create new file
    // Report progress during download
}
```

### Checksum Verification

```csharp
private async Task<bool> VerifyChecksumAsync(string filePath, string expectedSha256)
{
    using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
    using var sha256 = SHA256.Create();
    
    byte[] hashBytes = await sha256.ComputeHashAsync(fs);
    string computedHash = BitConverter.ToString(hashBytes)
        .Replace("-", "")
        .ToLowerInvariant();
    
    return computedHash.Equals(expectedSha256.ToLowerInvariant());
}
```

## Testing

### Unit Tests

Located in `Aura.Tests/DependencyManagerTests.cs`:
- Checksum verification (pass/fail)
- Component status detection
- File removal
- Manual instructions generation
- Resume logic with mocked HTTP responses

### Integration Tests

Located in `Aura.E2E/DependencyDownloadE2ETests.cs`:
- Manifest-driven flow
- Component lifecycle (install, verify, repair, remove)
- Post-install probe configuration
- Manual instructions workflow

### Running Tests

```bash
# Run all tests
dotnet test

# Run only dependency tests
dotnet test --filter "FullyQualifiedName~Dependency"

# Run E2E tests only
dotnet test Aura.E2E/Aura.E2E.csproj
```

## Available Components

1. **FFmpeg 6.0** (Required)
   - Video encoding and processing
   - ~80 MB
   - Probe: Verifies ffmpeg executable

2. **Ollama 0.1.19** (Optional)
   - Local LLM inference
   - ~500 MB
   - Probe: Checks API endpoint

3. **Ollama Model - llama3.1:8b** (Optional)
   - Language model for script generation
   - ~4.7 GB

4. **Stable Diffusion 1.5** (Optional, NVIDIA only)
   - Image generation
   - ~4.2 GB
   - Probe: Checks WebUI endpoint

5. **Stable Diffusion XL** (Optional, NVIDIA only)
   - Advanced image generation
   - ~6.9 GB

6. **CC0 Stock Pack** (Optional)
   - Stock images
   - ~1 GB

7. **CC0 Music Pack** (Optional)
   - Background music
   - ~512 MB

## Configuration

After installing components, configure their paths in **Settings → Local Providers**:
- FFmpeg path
- Ollama URL
- Stable Diffusion WebUI URL
- Output directories

## Troubleshooting

### Downloads Fail

1. Check internet connectivity
2. Verify URLs are accessible
3. Check disk space
4. Try manual installation

### Checksum Verification Fails

1. Click **Repair** to re-download
2. If repair fails, click **Remove** and reinstall
3. For persistent issues, use manual installation

### Components Don't Work After Install

1. Check **Settings → Local Providers**
2. Verify paths are correct
3. Test connections using Test buttons
4. Check post-install probe results

### Offline Installation

1. Click **Manual** button
2. Download files on a connected machine
3. Transfer files to offline machine
4. Place in specified directories
5. Use provided checksums to verify

## Security Considerations

1. **Checksum Verification**: All files are verified with SHA-256
2. **HTTPS URLs**: Use secure URLs when possible
3. **Local Storage**: Downloads stored in user's local app data
4. **No Credential Storage**: Downloads don't require authentication
5. **Sandbox Execution**: Probes run in isolated context

## License

See main repository LICENSE file.
