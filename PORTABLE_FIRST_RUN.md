# Portable First Run Guide

Welcome to **Aura Video Studio**! This guide will help you get started with the portable version on your first run.

## Quick Start

### 1. Extract the Archive

Extract the portable ZIP to a location of your choice (e.g., `C:\Aura` or `~/aura-video-studio`).

```
aura-video-studio/
├── Aura.Api.exe (or Aura.Api on Linux)
├── Aura.Web/
├── Tools/
├── AuraData/
└── README.md
```

### 2. Install FFmpeg

FFmpeg is required for video rendering. Choose one of the following methods:

#### Option A: Automatic Installation (Recommended)

1. Start the API server (see step 3 below)
2. Open the Web UI at http://localhost:5173
3. Navigate to **Settings** → **Downloads**
4. Click "Install FFmpeg" and wait for completion

#### Option B: Manual Installation

**Windows:**
1. Download FFmpeg from https://github.com/BtbN/FFmpeg-Builds/releases
2. Extract `ffmpeg.exe` and `ffprobe.exe` to the `Tools/ffmpeg/` folder
3. The API will automatically detect them on next startup

**Linux:**
```bash
# Ubuntu/Debian
sudo apt-get update
sudo apt-get install ffmpeg

# Or place binaries in Tools/ffmpeg/
```

#### Option C: Use System FFmpeg

If FFmpeg is already installed on your system:
1. Verify installation: `ffmpeg -version`
2. The API will automatically detect system FFmpeg if available

### 3. Start the API Backend

**Windows:**
```powershell
cd Aura.Api
.\Aura.Api.exe
```

**Linux:**
```bash
cd Aura.Api
dotnet Aura.Api.dll
# or
./Aura.Api
```

Wait for the message:
```
Now listening on: http://127.0.0.1:5005
```

### 4. Start the Web UI (Development)

In a **separate terminal**:

```bash
cd Aura.Web
npm install   # First time only
npm run dev
```

Open your browser to: **http://localhost:5173**

### 5. Verify Installation

#### Health Check

Visit http://localhost:5005/api/health/ready in your browser or run:

```bash
curl http://localhost:5005/api/health/ready
```

Expected response:
```json
{
  "status": "healthy",
  "checks": {
    "ffmpeg": "available",
    "storage": "writable"
  }
}
```

#### Provider Capabilities

Check which providers are available:

```bash
curl http://localhost:5005/api/providers/capabilities
```

Example response:
```json
[
  {
    "name": "StableDiffusion",
    "available": false,
    "reasonCodes": ["RequiresNvidiaGPU", "MissingApiKey:STABLE_KEY"],
    "requirements": {
      "needsKey": ["STABLE_KEY"],
      "needsGPU": "nvidia",
      "minVRAMMB": 6144,
      "os": ["windows", "linux"]
    }
  }
]
```

## Troubleshooting

### API Won't Start

| Issue | Solution |
|-------|----------|
| Port 5005 already in use | Change port: `set AURA_API_URL=http://127.0.0.1:5006` (Windows) or `export AURA_API_URL=http://127.0.0.1:5006` (Linux) |
| Missing dependencies | Install .NET 8 Runtime: https://dotnet.microsoft.com/download/dotnet/8.0 |
| Permission denied (Linux) | Run `chmod +x Aura.Api` |

### Health Check Fails

| Status | Reason | Fix |
|--------|--------|-----|
| `503 Service Unavailable` | API not fully started | Wait 10-15 seconds after API startup |
| `ffmpeg: unavailable` | FFmpeg not found | Install FFmpeg (see step 2 above) |
| `storage: read-only` | Insufficient permissions | Run with write permissions or change portable root location |

### Provider Capabilities Issues

| Reason Code | Meaning | Fix |
|-------------|---------|-----|
| `RequiresNvidiaGPU` | NVIDIA GPU required for this provider | Use alternative providers or install NVIDIA GPU |
| `MissingApiKey:*` | API key not configured | Add key in Settings → API Keys |
| `InsufficientVRAM` | GPU VRAM below minimum | Use lower quality settings or alternative providers |
| `UnsupportedOS` | OS not supported for this provider | Use alternative providers or switch OS |

### Web UI Won't Load

| Issue | Solution |
|-------|----------|
| `ECONNREFUSED` at http://localhost:5173 | Run `npm install` then `npm run dev` in Aura.Web directory |
| Blank page | Clear browser cache and refresh |
| API connection fails | Verify API is running at http://localhost:5005 |

### FFmpeg Issues

| Issue | Solution |
|-------|----------|
| FFmpeg not detected | Run `/api/dependencies/rescan` endpoint to refresh detection |
| FFmpeg version too old | Download latest from https://ffmpeg.org/download.html |
| Rendering fails | Check logs at `AuraData/logs/aura-api-*.log` |

## Configuration

### Custom Ports

**API Port:**
```bash
# Windows
set AURA_API_URL=http://127.0.0.1:5006
Aura.Api.exe

# Linux
export AURA_API_URL=http://127.0.0.1:5006
./Aura.Api
```

**Web UI Port:**

Edit `Aura.Web/vite.config.ts`:
```typescript
server: {
  port: 3000,  // Change from 5173
}
```

### Storage Locations

All data is stored relative to the portable root:

| Directory | Purpose |
|-----------|---------|
| `Tools/` | Downloaded dependencies (FFmpeg, Ollama, etc.) |
| `AuraData/` | Settings, manifests, configs |
| `AuraData/logs/` | Application logs |
| `Projects/` | Video projects and renders |
| `Downloads/` | Temporary downloads |

### API Keys

Add API keys via:
1. Web UI: Settings → API Keys
2. REST API: `POST /api/apikeys/save`
3. Manual: Edit `AuraData/apikeys.json` (Windows: encrypted with DPAPI)

## Advanced Setup

### Local AI Providers

For local AI without API keys:

**Ollama (LLM):**
```bash
# Install from https://ollama.ai
ollama pull llama3.1:8b
# API will auto-detect at http://localhost:11434
```

**Stable Diffusion (Image Generation):**
- Requires NVIDIA GPU with 6GB+ VRAM
- Install Automatic1111 WebUI: https://github.com/AUTOMATIC1111/stable-diffusion-webui
- Run on default port: http://127.0.0.1:7860
- Configure in Settings → Local Providers

### Production Deployment

See [DEPLOYMENT.md](./DEPLOYMENT.md) for production setup instructions.

## Getting Help

### If Generate Stalls or Shows No Progress

If clicking "Generate" doesn't show any visible progress or errors:

1. **Open the Render Status Drawer**
   - The drawer should open automatically when you start a job
   - If not, check the browser console for errors
   - Look for the drawer on the right side of the screen

2. **Run System Check**
   - Go to Settings → System → Run Check
   - This validates FFmpeg, disk space, and other dependencies
   - Fix any issues highlighted in red

3. **Copy the Correlation ID**
   - Every job has a correlation ID shown in the drawer header
   - Copy it for troubleshooting: `Job abc12345`
   - Search logs: `grep "abc12345" AuraData/logs/*.log`

4. **Try the Sample Preset**
   - Click "Try Sample" in the UI
   - This uses only local providers and bundled assets
   - Must succeed on clean machines with only FFmpeg
   - If this fails, check FFmpeg installation

5. **Check the Logs**
   - View `AuraData/logs/aura-api-*.log` for errors
   - Look for entries with your correlation ID
   - Common issues:
     - FFmpeg not found
     - Missing API keys
     - Insufficient disk space
     - Output directory not writable

6. **View Technical Details**
   - In the Render Status Drawer, expand "Technical Details" for failed steps
   - This shows FFmpeg stderr and full error context
   - Click "Copy" to save for support requests

### Common Issues and Fixes

| Issue | Solution |
|-------|----------|
| FFmpeg not found | Install FFmpeg (see step 2 above) |
| Missing API key | Add key in Settings → Providers |
| No disk space | Free up at least 5GB |
| Output directory error | Check permissions in Settings → Output |
| GPU encoding fails | Switch to software encoding (H.264) |

### Documentation

- **Jobs API:** [docs/jobs.md](./docs/jobs.md)
- **Error Codes:** [docs/errors.md](./docs/errors.md)
- **Issues:** https://github.com/Coffee285/aura-video-studio/issues
- **Logs:** Check `AuraData/logs/aura-api-*.log` for errors

## Next Steps

1. ✅ Verify health check passes
2. ✅ Check provider capabilities
3. 📹 Create your first video in the Web UI
4. ⚙️ Configure API keys and providers in Settings
5. 📚 Read the [User Guide](./docs/UX_GUIDE.md)

---

**Need more help?** See the full documentation in the [README.md](./README.md) and [INSTALL.md](./INSTALL.md).
