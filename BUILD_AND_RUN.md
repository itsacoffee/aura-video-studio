# Build and Run Guide - Aura Video Studio

## 🔧 Fixes Applied

### Issue: 404 Error When Running Portable Build

**Problem:** The Web UI returned 404 errors because the working directory was incorrect.

**Root Cause:** The launcher script was starting the API with:
```batch
start "" "Api\Aura.Api.exe"
```

This launched the executable but didn't change the working directory to the `Api` folder, causing it to look for `wwwroot` in the wrong location.

**Solution:** Updated the launcher to use:
```batch
start "" /D "Api" "Aura.Api.exe"
```

The `/D` flag sets the working directory to `Api`, ensuring the API finds `Api\wwwroot\` correctly.

### Additional Fix: API Endpoint Prefix

**Problem:** The Web UI was calling `/api/healthz` and `/api/capabilities` but the API served them at `/healthz` and `/capabilities`.

**Solution:** Added a route group with `/api` prefix to all API endpoints:
```csharp
var apiGroup = app.MapGroup("/api");
apiGroup.MapGet("/healthz", ...);
apiGroup.MapGet("/capabilities", ...);
// All other endpoints similarly updated
```

## 🚀 Quick Start - Portable Version (Recommended)

This is the easiest way to build and run Aura Video Studio.

### Prerequisites

- Windows 10/11
- PowerShell 5.1 or later
- .NET 8 SDK ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- Node.js 18+ ([Download](https://nodejs.org/))

### Build Steps

1. **Clone the repository** (if you haven't already):
   ```powershell
   git clone https://github.com/Coffee285/aura-video-studio.git
   cd aura-video-studio
   ```

2. **Run the portable build script**:
   ```powershell
   .\scripts\packaging\build-portable.ps1
   ```

   This will:
   - Build the .NET API (self-contained, no dependencies needed)
   - Build the React Web UI
   - Copy everything to `artifacts\portable\build\`
   - Create `AuraVideoStudio_Portable_x64.zip`

3. **Extract and run**:
   ```powershell
   # Extract the ZIP
   Expand-Archive artifacts\portable\AuraVideoStudio_Portable_x64.zip -DestinationPath test-run

   # Run the application
   cd test-run
   .\Launch.bat
   ```

4. **Use the application**:
   - The launcher will start the API server
   - After 3 seconds, it will open your browser to `http://127.0.0.1:5005`
   - You should see the Aura Video Studio welcome page

### Expected Output

When you run `Launch.bat`, you should see:
```
========================================
 Aura Video Studio - Portable Edition
========================================

Starting API server...
Waiting for server to start...

Opening web browser...

The application should open in your web browser.
If not, manually navigate to: http://127.0.0.1:5005
```

In the API server window, you should see:
```
[INFO] Serving static files from: C:\path\to\build\Api\wwwroot
[INFO] Now listening on: http://127.0.0.1:5005
[INFO] Application started. Press Ctrl+C to shut down.
```

**Key Success Indicator:** The first line should say "Serving static files from" - NOT "wwwroot directory not found"!

## 🛠️ Development Mode

For active development, you can run the API and Web UI separately:

### 1. Start the API Backend

```powershell
cd Aura.Api
dotnet run
```

Expected output:
```
[INFO] Serving static files from: C:\...\Aura.Api\wwwroot
[INFO] Now listening on: http://127.0.0.1:5005
```

### 2. Start the Web UI (in another terminal)

```powershell
cd Aura.Web
npm install
npm run dev
```

Expected output:
```
VITE v5.4.20  ready in 500 ms

➜  Local:   http://localhost:5173/
➜  Network: use --host to expose
```

### 3. Access the Application

- **Development:** Open http://localhost:5173 (Web UI dev server with hot reload)
- **Production-like:** Open http://127.0.0.1:5005 (served by API)

## 🧪 Testing

### Run Unit Tests

```powershell
dotnet test Aura.Tests/Aura.Tests.csproj
```

Expected: 84+ tests passing

### Run E2E Tests

```powershell
dotnet test Aura.E2E/Aura.E2E.csproj
```

Expected: 8+ tests passing

### Test the API Endpoints

```powershell
# Health check
curl http://127.0.0.1:5005/api/healthz

# System capabilities
curl http://127.0.0.1:5005/api/capabilities

# Available profiles
curl http://127.0.0.1:5005/api/profiles/list
```

## 📊 What's Included

The application includes:

### Backend API (Aura.Api)
- ✅ Health check endpoint
- ✅ Hardware detection and capabilities
- ✅ Script generation (rule-based, free)
- ✅ Text-to-speech (Windows SAPI, free)
- ✅ Video composition (FFmpeg)
- ✅ Settings management
- ✅ Render queue management
- ✅ Downloads manifest

### Web UI (Aura.Web)
- ✅ Welcome page with system status
- ✅ Create page (step-by-step video creation wizard)
- ✅ Dashboard (overview and quick actions)
- ✅ Render page (export settings)
- ✅ Publish page (YouTube metadata)
- ✅ Downloads page (dependency management)
- ✅ Settings page (API keys, preferences)

### Core Features (Aura.Core)
- ✅ Hardware detection (CPU, RAM, GPU)
- ✅ Provider system (LLM, TTS, Video)
- ✅ Video orchestration pipeline
- ✅ Timeline builder
- ✅ Render presets (YouTube 1080p, 4K, Shorts)
- ✅ Audio processing and normalization

## 🐛 Troubleshooting

### Issue: "wwwroot directory not found"

**Symptom:**
```
[WARN] wwwroot directory not found at: C:\...\build\wwwroot
[WARN] Static file serving is disabled. Web UI will not be available.
```

**Solution:** This means you're running an old build. Rebuild using the latest code:
```powershell
.\scripts\packaging\build-portable.ps1
```

### Issue: 404 Error in Browser

**Symptom:** Browser shows "404 - Not Found" when accessing http://127.0.0.1:5005

**Causes:**
1. Old launcher script (doesn't set working directory correctly)
2. Missing wwwroot folder
3. API not finding static files

**Solution:**
1. Rebuild the portable version with the latest scripts
2. Verify the directory structure:
   ```
   build\
   ├── Api\
   │   ├── Aura.Api.exe
   │   └── wwwroot\
   │       ├── index.html
   │       └── assets\
   ├── Launch.bat
   └── README.md
   ```

### Issue: API endpoints return 404

**Symptom:** Web UI shows errors like "Failed to fetch system info"

**Solution:** Make sure you're using the latest API code that includes the `/api` prefix. Rebuild:
```powershell
cd Aura.Api
dotnet build
```

### Issue: Port 5005 already in use

**Symptom:**
```
[ERROR] Failed to bind to address http://127.0.0.1:5005: address already in use
```

**Solution:**
1. Close any other instances of Aura.Api.exe
2. Or change the port in `Aura.Api/Program.cs`:
   ```csharp
   builder.WebHost.UseUrls("http://127.0.0.1:5006");
   ```

## 📝 Next Steps

After confirming the application works:

1. **Create your first video:**
   - Click "Create Video" on the welcome page
   - Fill in the brief (topic, audience, tone)
   - Set duration and pacing
   - Click "Generate Video"

2. **Configure providers:**
   - Go to Settings
   - Add API keys for pro providers (optional)
   - Configure hardware preferences

3. **Explore features:**
   - Check hardware capabilities on the dashboard
   - View render queue
   - Manage downloads

## 🆘 Getting Help

If you're still experiencing issues:

1. **Check the console output** - The API logs show exactly what's happening
2. **Verify the directory structure** - Make sure wwwroot is in the right place
3. **Check file permissions** - Ensure the launcher can execute Aura.Api.exe
4. **Review the logs** - API logs are in `logs/aura-api-*.log`

## 📚 Additional Documentation

- [README.md](./README.md) - Project overview and architecture
- [ARCHITECTURE.md](./ARCHITECTURE.md) - System design and components
- [PORTABLE.md](./PORTABLE.md) - User guide for the portable version
- [INSTALL.md](./INSTALL.md) - Detailed installation instructions
- [Aura.Api/README.md](./Aura.Api/README.md) - API documentation
- [Aura.Web/README.md](./Aura.Web/README.md) - Web UI documentation

---

**Note:** All the fixes mentioned above are already applied to the codebase. Simply rebuild and run to get the working version!
