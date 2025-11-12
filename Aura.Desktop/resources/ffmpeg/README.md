# FFmpeg Binaries Directory

This directory contains FFmpeg binaries that are bundled with the Aura Video Studio Desktop application.

## Directory Structure

```
ffmpeg/
├── win-x64/
│   └── bin/
│       ├── ffmpeg.exe
│       ├── ffprobe.exe
│       └── ffplay.exe (optional)
├── osx-x64/
│   └── bin/
│       ├── ffmpeg
│       ├── ffprobe
│       └── ffplay (optional)
└── linux-x64/
    └── bin/
        ├── ffmpeg
        ├── ffprobe
        └── ffplay (optional)
```

## Windows FFmpeg Installation

### Automatic Download (Development)

For development, use the PowerShell script to automatically download FFmpeg:

```powershell
cd Aura.Desktop
.\scripts\download-ffmpeg-windows.ps1
```

Options:
- `-Force`: Force re-download even if FFmpeg exists
- `-Help`: Show help information

### Manual Installation

If you prefer to install FFmpeg manually:

1. Download FFmpeg GPL build from: https://github.com/BtbN/FFmpeg-Builds/releases/latest
   - File: `ffmpeg-master-latest-win64-gpl.zip`
   - Size: ~140MB

2. Extract the archive

3. Copy the binaries:
   - From: `ffmpeg-master-latest-win64-gpl\bin\`
   - To: `Aura.Desktop\resources\ffmpeg\win-x64\bin\`
   - Required files: `ffmpeg.exe`, `ffprobe.exe`

4. Verify installation:
   ```powershell
   .\resources\ffmpeg\win-x64\bin\ffmpeg.exe -version
   ```

## Production Bundling

When building the Electron app for distribution, FFmpeg binaries are automatically bundled via electron-builder configuration in `package.json`:

```json
"extraResources": [
  {
    "from": "resources/ffmpeg",
    "to": "ffmpeg",
    "filter": ["**/*"]
  }
]
```

The binaries are unpacked during installation and located at:
- **Development**: `<project>/Aura.Desktop/resources/ffmpeg/win-x64/bin/`
- **Production**: `<app>/resources/ffmpeg/win-x64/bin/`

## Path Detection

The application detects FFmpeg using the following priority:

1. **Electron Environment Variables** (highest priority)
   - `FFMPEG_PATH` - Set by Electron's backend-service.js
   - `FFMPEG_BINARIES_PATH` - Alternative path

2. **Configured Path**
   - User-configured path in settings
   - Managed installation path

3. **Dependencies Directory**
   - `%LOCALAPPDATA%\Aura\dependencies\bin\ffmpeg.exe`

4. **Tools Directory**
   - `%LOCALAPPDATA%\Aura\Tools\ffmpeg\{version}\bin\ffmpeg.exe`

5. **Windows Registry** (Windows only)
   - `HKLM\SOFTWARE\FFmpeg`
   - `HKLM\SOFTWARE\WOW6432Node\FFmpeg`
   - `HKCU\SOFTWARE\FFmpeg`

6. **System PATH**
   - Standard PATH environment variable lookup

## Hardware Acceleration

The bundled FFmpeg build supports hardware acceleration on Windows:

- **NVIDIA**: NVENC (h264_nvenc, hevc_nvenc)
- **AMD**: AMF (h264_amf, hevc_amf)
- **Intel**: QuickSync (h264_qsv, hevc_qsv)

Hardware acceleration is automatically detected and used when available.

## License

The FFmpeg binaries are licensed under the **GPL v3** license. The GPL build includes all codecs and features.

For more information:
- FFmpeg License: https://www.ffmpeg.org/legal.html
- GPL v3: https://www.gnu.org/licenses/gpl-3.0.html

## Troubleshooting

### FFmpeg Not Found

If the application cannot find FFmpeg:

1. Check the logs for detection attempts:
   - `%LOCALAPPDATA%\Aura\Logs\ffmpeg\`

2. Verify FFmpeg exists:
   ```powershell
   Test-Path .\resources\ffmpeg\win-x64\bin\ffmpeg.exe
   ```

3. Run the download script:
   ```powershell
   .\scripts\download-ffmpeg-windows.ps1 -Force
   ```

4. Check environment variables:
   ```powershell
   $env:FFMPEG_PATH
   $env:FFMPEG_BINARIES_PATH
   ```

### Version Check

To verify the FFmpeg version:

```powershell
.\resources\ffmpeg\win-x64\bin\ffmpeg.exe -version
```

Expected output should include:
- FFmpeg version information
- Configuration flags (including `--enable-libx264`)
- Supported encoders and decoders

### Binary Verification

Verify the binaries are not corrupted:

```powershell
# Check file sizes (approximate)
Get-ChildItem .\resources\ffmpeg\win-x64\bin\ | Select-Object Name, Length

# Expected sizes (may vary by version):
# ffmpeg.exe:  ~110-130 MB
# ffprobe.exe: ~110-130 MB
```

## Additional Resources

- **FFmpeg Documentation**: https://ffmpeg.org/documentation.html
- **FFmpeg Windows Builds**: https://github.com/BtbN/FFmpeg-Builds
- **Aura FFmpeg Implementation**: See `FFMPEG_INTEGRATION_COMPLETE.md` in project root
- **Windows Integration Details**: See `PR_ELECTRON_003_WINDOWS_FFMPEG_IMPLEMENTATION.md`
