# Aura Video Studio

[![Build Validation](https://github.com/Saiyan9001/aura-video-studio/actions/workflows/build-validation.yml/badge.svg)](https://github.com/Saiyan9001/aura-video-studio/actions/workflows/build-validation.yml)
[![No Placeholders](https://github.com/Saiyan9001/aura-video-studio/actions/workflows/no-placeholders.yml/badge.svg)](https://github.com/Saiyan9001/aura-video-studio/actions/workflows/no-placeholders.yml)

## 🎯 Platform Scope

**Aura Video Studio is a Windows 11 (x64) application.**

This software is designed, built, and tested exclusively for **Windows 11 (64-bit)** as the end-user platform. While developers may work in other environments for backend/API development, the complete application stack—including the web UI, build process, and distribution—targets Windows 11 only.

**Why Windows 11 only?**
- **Native toolchain compatibility**: Optimal integration with .NET 8, WinUI 3, and Windows-specific APIs
- **Predictable dependencies**: Windows-native binaries (FFmpeg, NVENC, etc.) with guaranteed compatibility
- **Hardware acceleration**: First-class support for NVIDIA NVENC, AMD AMF, and Intel QSV
- **Simplified distribution**: Portable ZIP format works seamlessly on Windows without cross-platform complexity
- **Consistent user experience**: Single platform = fewer edge cases, better stability, faster iteration

## 🚀 Quick Start for Developers

### Prerequisites
- Windows 11 (64-bit)
- Node.js 18.0.0+ (18.18.0 recommended)
- .NET 8 SDK
- Git for Windows

### Setup

```bash
# Clone the repository
git clone https://github.com/Saiyan9001/aura-video-studio.git
cd aura-video-studio

# Install frontend dependencies
cd Aura.Web
npm install

# Build frontend
npm run build

# Build backend
cd ..
dotnet build Aura.sln --configuration Release

# Run the application
cd Aura.Api
dotnet run
```

**⚠️ First Time Running?**  
📖 **[First Run Guide](FIRST_RUN_GUIDE.md)** - Essential setup steps to avoid white screen issues

**For detailed setup instructions, troubleshooting, and development workflow:**  
📖 **[Complete Build Guide](docs/developer/BUILD_GUIDE.md)**

### Troubleshooting

**Common issues:**
- **White screen / "Application Failed to Initialize"**: Frontend not built - see [First Run Guide](FIRST_RUN_GUIDE.md)
- **"Path too long" error**: Enable Windows long path support (see BUILD_GUIDE.md)
- **"Module not found"**: Delete `node_modules` and run `npm install` again
- **"Permission denied"**: Run terminal as Administrator or check antivirus
- **TypeScript errors**: Run `npm install` to get latest type definitions

See [BUILD_GUIDE.md](docs/developer/BUILD_GUIDE.md) for complete troubleshooting guide.

## 🚀 Implementation Status

**Core Infrastructure: ✅ COMPLETE**  
**Web-Based Architecture: ✅ IMPLEMENTED**

This repository contains:
- ✅ 143 tests passing (100% pass rate)
- ✅ ~7,500+ lines of production code
- ✅ **Aura.Api** - ASP.NET Core backend with RESTful endpoints
- ✅ **Aura.Web** - React + Vite + TypeScript + Fluent UI frontend
- ✅ **Advanced Prompt Engineering** - Customize AI prompts, few-shot examples, chain-of-thought
- ✅ Complete hardware detection with NVIDIA-only SD gating
- ✅ Provider system with free/pro mixing and automatic fallback
- ✅ FFmpeg render pipeline with multi-encoder support
- ✅ Audio processing with LUFS normalization
- ✅ Subtitle generation (SRT/VTT)
- ✅ **Windows-focused packaging** - Portable ZIP distribution
- ✅ Dependency manifest with SHA-256 verification
- ✅ SBOM generation and license attributions

## 📐 Architecture

**Web-based UI architecture:**
- **Aura.Core** - Business logic (.NET 8)
- **Aura.Providers** - Provider implementations
- **Aura.Api** - ASP.NET Core backend API (runs on http://127.0.0.1:5005)
- **Aura.Web** - React + Fluent UI frontend (dev on port 5173)
- **Aura.App** - WinUI 3 standalone app (coexists as alternative)

See [Architecture Documentation](./docs/architecture/ARCHITECTURE.md) for complete details.

## 🏥 Health Checks & Monitoring

The API provides production-grade health check endpoints for monitoring system status and dependencies:

### Health Endpoints

- **`/health/live`** - Liveness probe (returns 200 if app is running)
  - Use for Kubernetes liveness probes
  - No dependency checks - just confirms process is alive

- **`/health/ready`** - Readiness probe (validates all dependencies)
  - Checks FFmpeg availability
  - Validates disk space (configurable thresholds)
  - Detects GPU hardware and capabilities
  - Returns detailed JSON status for each check
  - Use for Kubernetes readiness probes and load balancer health checks

### Example Response

```json
{
  "status": "healthy",
  "timestamp": "2025-10-28T04:00:00Z",
  "checks": [
    {
      "name": "Dependencies",
      "status": "healthy",
      "description": "All dependencies available",
      "data": {
        "ffmpeg_available": true,
        "gpu_available": true,
        "nvenc_available": true
      }
    },
    {
      "name": "DiskSpace",
      "status": "healthy",
      "description": "Sufficient disk space: 125.50 GB free",
      "data": {
        "free_gb": 125.50,
        "total_gb": 500.00
      }
    }
  ]
}
```

📖 **Documentation:** [docs/api/health.md](./docs/api/health.md)

## 🎨 Advanced Prompt Engineering

Aura Video Studio features a sophisticated prompt engineering framework that lets you customize how AI generates video scripts.

### Features

- **Custom Instructions:** Add your own guidelines to influence script generation
- **Few-Shot Examples:** 15 curated examples across 5 video types (Educational, Entertainment, Tutorial, Documentary, Promotional)
- **Prompt Versions:** Choose from multiple optimization strategies:
  - `default-v1` - Balanced quality for general use
  - `high-engagement-v1` - Optimized for maximum viewer retention
  - `educational-deep-v1` - Comprehensive explanations for learning content
- **Chain-of-Thought:** Break generation into 3 iterative stages (Topic Analysis → Outline → Full Script)
- **Security Validation:** Built-in protection against prompt injection attacks
- **Preset Management:** Save and reuse successful prompt configurations

### Quick Example

```typescript
// Generate preview with custom prompt
const preview = await fetch('http://localhost:5005/api/prompts/preview', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    topic: 'Machine Learning Basics',
    tone: 'informative',
    targetDurationMinutes: 3,
    promptModifiers: {
      additionalInstructions: 'Focus on practical examples',
      promptVersion: 'educational-deep-v1',
      exampleStyle: 'Science Explainer'
    }
  })
});
```

### Documentation

- 📖 **[User Guide](PROMPT_CUSTOMIZATION_USER_GUIDE.md)** - Complete guide for end users
- 📖 **[API Documentation](PROMPT_ENGINEERING_API.md)** - Developer API reference
- 📖 **[Implementation Details](PROMPT_ENGINEERING_IMPLEMENTATION.md)** - Technical architecture

### API Endpoints

- `POST /api/prompts/preview` - Generate prompt preview with variable substitutions
- `GET /api/prompts/list-examples` - Get few-shot examples (optionally filtered by type)
- `GET /api/prompts/versions` - List available prompt versions
- `POST /api/prompts/validate-instructions` - Validate custom instructions for security

## 🛡️ Rate Limiting

API requests are rate-limited to protect against abuse and accidental DoS:

### Rate Limit Policies

| Endpoint | Limit | Window |
|----------|-------|--------|
| POST /api/jobs | 10 requests | 1 minute |
| POST /api/quick/demo | 5 requests | 1 minute |
| POST /api/script | 20 requests | 1 minute |
| General (catch-all) | 100 requests | 1 minute |

**Health endpoints are exempt** from rate limiting to ensure monitoring systems can check frequently.

### Rate Limit Headers

Every response includes rate limit information:

```http
X-RateLimit-Limit: 10
X-RateLimit-Remaining: 7
X-RateLimit-Reset: 1698451260
```

When limit is exceeded, returns **HTTP 429 Too Many Requests** with `Retry-After` header.

📖 **Documentation:** [docs/api/rate-limits.md](./docs/api/rate-limits.md)

## ⚙️ System Requirements

**Minimum Requirements:**
- **Operating System**: Windows 11 (64-bit) - **Required**
- **.NET Runtime**: 8.0 (included in portable distribution)
- **Node.js**: 18.x or 20.x (for development only)
- **npm**: 9.x or 10.x (for development only)
- **RAM**: 8 GB
- **Storage**: 5 GB free space

**Recommended:**
- Windows 11 22H2 or later
- 16 GB RAM
- NVIDIA GPU with 6+ GB VRAM for hardware acceleration
- SSD for faster build and render times

## 🔧 Backend Dependencies

### Critical NuGet Packages (.NET 8.0)

**Core Framework:**
- Microsoft.Extensions.* (9.0.10) - Dependency injection, logging, HTTP client factory
- System.Management (9.0.10) - Hardware detection and system info
- System.Text.Json (9.0.10) - JSON serialization

**Web API (Aura.Api):**
- Microsoft.AspNetCore.OpenApi (8.0.20) - OpenAPI/Swagger support (.NET 8 compatible)
- Swashbuckle.AspNetCore (9.0.6) - Swagger UI and API documentation
- Serilog.AspNetCore (9.0.0) - Structured logging
- Serilog.Sinks.File (7.0.0) - File-based logging

**Testing:**
- xunit (2.9.3) - Unit testing framework
- Moq (4.20.72) - Mocking framework
- Microsoft.AspNetCore.Mvc.Testing (8.0.11) - Integration testing
- Microsoft.NET.Test.Sdk (18.0.0) - Test execution
- coverlet.collector (6.0.4) - Code coverage

### Version Requirements
- **.NET SDK**: 8.0 or later (for development)
- **Target Framework**: net8.0
- **Node.js**: 18.x or 20.x (for frontend development)
- **npm**: 9.x or 10.x (for frontend development)

**Development Note**: While the backend (.NET) components can be built on any platform with .NET 8 SDK, the complete application—including WinUI 3 app (Aura.App) and final packaging—requires Windows 11.

### Security
All packages are regularly audited for vulnerabilities using `dotnet list package --vulnerable`. Last audit: 2025-10-22 - **No vulnerabilities found**.

### Package Updates
To update packages (on Windows):
```powershell
# Check for outdated packages
dotnet list package --outdated

# Check for vulnerable packages
dotnet list package --vulnerable

# Update specific package
dotnet add package <PackageName>
```

## 📦 Distribution Policy

**Aura Video Studio follows a portable-only distribution model.**

- ✅ **Portable ZIP** - Self-contained, no-install distribution (primary and only release format)
- ❌ **MSIX/APPX packages** - Not supported
- ❌ **Traditional installers (EXE)** - Not supported

This policy ensures maximum flexibility and compatibility. Users can extract and run the application anywhere without system-level installation or administrator privileges.

For more information, see [Portable Mode Guide](./docs/user-guide/PORTABLE.md)

## 📚 Documentation

Comprehensive documentation is available in the [docs/](./docs/) directory. See the [Documentation Index](./docs/INDEX.md) for a complete map of all documentation.

### Quick Links

**For Users:**
- [Quick Start Guide](./docs/user-guide/QUICKSTART.md) - Get started quickly
- [Portable Mode Guide](./docs/workflows/PORTABLE_MODE_GUIDE.md) - Running in portable mode
- [User Guides](./docs/user-guide/) - Feature-specific user documentation
- [Troubleshooting](./docs/troubleshooting/README.md) - Common issues and solutions

**For Developers:**
- [Contributing Guide](./CONTRIBUTING.md) - Development guidelines and standards
- [Build & Run Guide](./docs/developer/BUILD_AND_RUN.md) - Development setup
- [Architecture Documentation](./docs/architecture/ARCHITECTURE.md) - System architecture overview
- [API Reference](./docs/api/README.md) - REST API documentation
- [Security Policy](./SECURITY.md) - Security practices and reporting

**Additional Resources:**
- [Workflows](./docs/workflows/README.md) - Common workflows and use cases
- [Best Practices](./docs/best-practices/README.md) - Optimization and guidelines
- [CI/CD Documentation](./docs/CI.md) - Continuous integration setup

### Building Documentation

Generate API documentation from code (Windows):

```powershell
# .NET API documentation (DocFX)
dotnet tool install -g docfx
docfx docfx.json
docfx serve _site

# TypeScript API documentation
cd Aura.Web
npm run docs
```

Documentation is automatically built and deployed to GitHub Pages on every commit to `main`.

## 🚦 Quick Start (Windows 11)

**Prerequisites:**
1. Windows 11 (64-bit) - **Required**
2. Download and install [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
3. Download and install [Node.js 18.x or 20.x](https://nodejs.org/)

**Development Mode (for developers):**

```powershell
# 1. Clone the repository
git clone https://github.com/Saiyan9001/aura-video-studio.git
cd aura-video-studio

# 2. Start the backend API
cd Aura.Api
dotnet run

# 3. In a new PowerShell window, start the web UI
cd Aura.Web
npm install
npm run dev

# 4. Open your browser to http://localhost:5173
```

**Production/Portable Mode (end users):**

```powershell
# 1. Download the latest portable release ZIP from GitHub Releases

# 2. Extract to a folder (e.g., C:\AuraStudio)

# 3. Navigate to the folder
cd C:\AuraStudio

# 4. Run the API server
.\Aura.Api.exe
# Or: dotnet Aura.Api.dll

# 5. Open your browser to http://127.0.0.1:5005
```

The portable build includes the frontend in the `wwwroot` directory. No separate web server is needed.

## 🔧 Troubleshooting

### White Page / Blank Screen

**Symptom:** Navigating to http://127.0.0.1:5005 shows a white/blank page with no UI.

**Diagnosis Steps:**

1. **Visit the diagnostic page:** http://127.0.0.1:5005/diag
   - Check "Static File Hosting Status" - should show ✅ for wwwroot, index.html, and assets
   - Check that "Total files in wwwroot" is > 0
   - Click "Test JS File" and "Test CSS File" buttons - should show ✓ Asset Fetch Test

2. **Open Browser DevTools:** Press `F12` or `Ctrl+Shift+I`
   - **Console Tab:** Look for red error messages
     - CSP violations: "Refused to execute script..."
     - Missing files: "Failed to load resource: 404"
     - CORS errors: "blocked by CORS policy"
   - **Network Tab:** 
     - Filter to "JS" - all JavaScript files should show status 200
     - Filter to "CSS" - all CSS files should show status 200
     - Look for 404 errors on any `/assets/` files

**Common Causes & Fixes:**

| Issue | Cause | Fix |
|-------|-------|-----|
| wwwroot missing | Frontend not built/copied | Run: `cd Aura.Web && npm run build`, then copy `dist/*` to `Aura.Api/wwwroot/` |
| Assets return 404 | Static file serving not configured | Check server logs for "Static UI: ENABLED" message |
| Scripts blocked | Content Security Policy too strict | Check Console for CSP errors; see CSP section below |
| JS files have wrong MIME | Content-Type not set correctly | Verify server logs show proper MIME type mapping |

**Quick Fixes:**

```powershell
# Fix 1: Rebuild and copy frontend
cd Aura.Web
npm run build
Copy-Item -Path ./dist/* -Destination ../Aura.Api/wwwroot/ -Recurse -Force

# Fix 2: Clear browser cache
# Press Ctrl+Shift+Delete → Clear "Cached images and files" → Clear data

# Fix 3: Hard refresh
# Press Ctrl+F5 or Ctrl+Shift+R

# Fix 4: Verify API is running
# Open http://127.0.0.1:5005/api/healthz - should return JSON

# Fix 5: Check server logs
# Look for "Static UI: ENABLED" and "SPA fallback: ACTIVE" messages
```

### Deep Link Refresh Issues

**Symptom:** Navigating to a route like `/dashboard` works, but refreshing the page shows 404 or blank page.

**Cause:** SPA fallback not active - server doesn't return index.html for client-side routes.

**Fix:**

1. Check server logs for: `SPA fallback configured: All unmatched routes will serve index.html`
2. Test manually:
   ```powershell
   # Should return HTML, not 404
   Invoke-WebRequest -Uri "http://127.0.0.1:5005/dashboard"
   ```
3. If still failing, the fallback may not be configured correctly in Program.cs

**Alternative (Fail-safe):** Use HashRouter instead of BrowserRouter
- Pros: Works without server-side configuration; refresh always works
- Cons: URLs have `#` in them (e.g., `http://127.0.0.1:5005/#/dashboard`)
- Implementation: In `Aura.Web/src/App.tsx`, replace `<BrowserRouter>` with `<HashRouter>` from react-router-dom

### Service Worker or Stale Cache

**Symptom:** App shows old/blank content even after updates; changes don't appear.

**Diagnosis:**

1. Visit http://127.0.0.1:5005/diag and click "Check Service Worker"
2. Or: Open DevTools → Application tab → Service Workers
   - If you see any registered service workers, they may be caching old content

**Fix:**

```text
1. Open DevTools (F12)
2. Go to Application tab
3. Click "Service Workers" in the left sidebar
4. Click "Unregister" next to any registered workers
5. Go to "Storage" in the left sidebar
6. Click "Clear site data"
7. Close DevTools and hard refresh (Ctrl+F5)
```

### Content Security Policy (CSP) Blocking Scripts

**Symptom:** Console shows: `Refused to execute inline script because it violates the following Content Security Policy directive...`

**Cause:** CSP headers are too restrictive and block bundled scripts.

**Fix:**

The current implementation allows scripts from 'self' (same origin). If you still see CSP errors:

1. Check for conflicting CSP meta tags in index.html (there should be none)
2. Verify server logs don't show custom CSP headers being added
3. Temporarily disable CSP for testing:
   - Add `<meta http-equiv="Content-Security-Policy" content="default-src * 'unsafe-inline' 'unsafe-eval'; script-src * 'unsafe-inline' 'unsafe-eval';">` to index.html
   - **Note:** Only for debugging, not for production

### API Not Reachable

**Symptom:** Frontend loads but can't connect to API; network requests fail.

**Diagnosis:**

```powershell
# Test 1: Check if API is listening
Invoke-WebRequest -Uri "http://127.0.0.1:5005/api/healthz"
# Should return: {"status":"healthy",...}

# Test 2: Check root health (includes static hosting status)
Invoke-WebRequest -Uri "http://127.0.0.1:5005/healthz"
# Should return: {"status":"healthy","staticHosting":"ready"}

# Test 3: Check if port is in use
Get-NetTCPConnection -LocalPort 5005 -ErrorAction SilentlyContinue
# Should show LISTENING state
```

**Common Causes:**

- Port 5005 already in use by another app → Change port in environment variable: `$env:ASPNETCORE_URLS="http://127.0.0.1:5006"`
- Firewall blocking localhost connections → Add exception in Windows Firewall
- API crashed during startup → Check logs in `./logs/` directory

### Build Failures

**Frontend build fails:**

```powershell
# Clean and reinstall dependencies
cd Aura.Web
Remove-Item -Recurse -Force node_modules
Remove-Item package-lock.json
npm install
npm run build
```

**Backend build fails:**

```powershell
# Restore packages and rebuild
dotnet clean
dotnet restore
dotnet build --configuration Release
```

### Getting Help

If the above steps don't resolve your issue:

1. **Collect diagnostic information:**
   - Visit http://127.0.0.1:5005/diag and screenshot the page
   - Copy Console errors from DevTools (F12 → Console)
   - Copy Network errors from DevTools (F12 → Network)
   - Copy relevant server logs from `./logs/` directory

2. **Check existing issues:** https://github.com/Saiyan9001/aura-video-studio/issues

3. **Create a new issue** with:
   - OS version (Windows 11 build number)
   - Steps to reproduce
   - Diagnostic information from step 1
   - Expected vs actual behavior

That's it! The application should now be running.

### Building Portable Distribution (Windows)
```powershell
# Build portable ZIP (recommended for distribution)
.\scripts\packaging\build-portable.ps1

# Output: Portable ZIP in artifacts/portable/
```

For detailed build instructions, see [BUILD_AND_RUN.md](./BUILD_AND_RUN.md)

### Smoke Testing (Windows)

Run startup sanity checks to ensure the application is healthy:

```powershell
# Windows (Recommended)
.\scripts\smoke\start_and_probe.ps1
```

**Note**: Bash versions of scripts (`.sh` files) exist for backend-only development on other platforms but are **not supported** for end-user scenarios. Windows 11 is the only supported platform for the complete application.

The smoke test script:
- Builds the solution
- Runs core tests
- Starts the API in background
- Probes critical endpoints (health, capabilities, queue)
- Monitors logs for exceptions
- Returns exit code 0 on success

See [scripts/smoke/README.md](./scripts/smoke/README.md) for detailed usage.

## 🔧 Troubleshooting (Windows)

### Build Issues

**Problem: `dotnet build` fails**
- **Solution**: Ensure .NET 8 SDK is installed:
  ```powershell
  dotnet --version  # Should be 8.0.x or higher
  ```

**Problem: Package restore fails**
- **Solution**: Clear NuGet cache and restore:
  ```powershell
  dotnet nuget locals all --clear
  dotnet restore
  ```

**Problem: npm install fails**
- **Solution**: Ensure Node.js 18.x or 20.x is installed:
  ```powershell
  node --version  # Should be v18.x or v20.x
  npm --version   # Should be 9.x or 10.x
  ```

**Problem: API fails to start with DI lifetime errors**
- **Cause**: Service lifetime mismatch (e.g., singleton consuming scoped service)
- **Solution**: Check that services are registered with compatible lifetimes. Updated packages may enforce stricter validation.

**Problem: Outdated package warnings**
- **Solution**: Check for updates:
  ```powershell
  dotnet list package --outdated
  ```
- Note: Only update to versions compatible with .NET 8.0. Some v9.x packages require .NET 9.0.

### Runtime Issues

**Problem: API starts but endpoints return 404**
- **Check:** Verify API is running on correct port (default: http://localhost:5272 in dev)
- **Check:** Access Swagger UI at `/swagger` to see available endpoints
- **Check:** Review logs in `Logs/` directory for routing issues

**Problem: Missing FFmpeg or other tools**
- **Solution:** Download required tools using the Engine Manager in the UI or API
- **Manual:** Place FFmpeg binaries in `Tools/ffmpeg/` directory

**See detailed documentation:**
- [PORTABLE_FIRST_RUN.md](./PORTABLE_FIRST_RUN.md) - **🎯 First-time setup guide for portable version**
- [INSTALL.md](./INSTALL.md) - **Build and installation guide**
- [LOCAL_PROVIDERS_SETUP.md](./LOCAL_PROVIDERS_SETUP.md) - **How to set up local AI providers (Stable Diffusion, Ollama, FFmpeg)**
- [PORTABLE.md](./PORTABLE.md) - **User guide for portable version**
- [scripts/smoke/README.md](./scripts/smoke/README.md) - **Smoke test scripts for sanity checks**
- [IMPLEMENTATION_SUMMARY.md](./IMPLEMENTATION_SUMMARY.md) - Original implementation details
- [ARCHITECTURE.md](./ARCHITECTURE.md) - Complete architecture overview
- [Aura.Api/README.md](./Aura.Api/README.md) - Backend API documentation
- [Aura.Web/README.md](./Aura.Web/README.md) - Frontend UI documentation
- [scripts/packaging/README.md](./scripts/packaging/README.md) - Packaging guide

---

# GitHub Copilot Super‑Prompt

**Goal:** Generate a polished Windows 11 desktop application that creates complete YouTube videos (script → voiceover → visuals/B‑roll → captions → music/SFX → thumbnail → rendered video → metadata → optional upload) from a simple user brief. The app must be beginner‑friendly, beautifully designed, and fully automated with smart defaults. It must always offer a **Free Path** (no paid API keys required) and optionally unlock **Pro Providers** via user‑entered API keys.

---

## Product Name

**Aura Video Studio (Windows 11)**

---

## Platform & Tech Stack (strict)

* **OS:** Windows 11 only.
* **Framework:** **WinUI 3** (Windows App SDK, .NET 8, C#), XAML for UI with Fluent Design (Mica/Acrylic). No Electron.
* **Video:** `FFmpeg` (bundled portable binaries). Use `Xabe.FFmpeg` or `FFMpegCore` for orchestration.
* **Audio:** `NAudio` for mixing/normalization.
* **Graphics/Compositing:** `SkiaSharp` for thumbnail/text overlays/waveforms.
* **JSON/HTTP:** `System.Text.Json`, `HttpClientFactory`.
* **DI & MVVM:** `CommunityToolkit.Mvvm`, `Microsoft.Extensions.Hosting`.
* **Packaging:** Portable ZIP distribution (no MSIX/installers).
* **Unit Tests:** xUnit + FluentAssertions; 90%+ coverage on core services.
* **E2E Smoke:** Minimal UI test via WinAppDriver (smoke render, one full free‑path render).

> Deliver a **solution** with projects: `Aura.App` (WinUI), `Aura.Core` (business logic), `Aura.Providers` (AI providers), `Aura.Tests` (unit), `Aura.E2E` (smoke). Include a `scripts/` folder with `ffmpeg/` binaries.

---

## UX & Visual Design

Use Fluent/WinUI styles with subtle elevation, rounded corners, and dynamic Mica. Smooth transitions and progress indicators are essential. Provide a left nav, a main canvas, and a right context panel.

### Simplicity First (new)

* **One clear path:** Big primary buttons, descriptive subtitles, disabled options explained with tooltips and *Learn More* links.
* **Progressive disclosure:** Advanced toggles hidden behind *Show Advanced* in each step.
* **Live estimates:** Everywhere—time to render, VRAM usage hints, disk space projections.
* **Inline notes & tips:** Small “i” glyphs reveal short 1–2 line guidance; avoid walls of text.
* **Error clarity:** Human language, clear causes, next steps; copy‑to‑clipboard error details.

### Layout & Ergonomics (new)

* **Resizable panes** with snap points; remember user sizes per view.
* **Keyboard shortcuts:** Space = play/pause, J/K/L shuttle, +/- zoom timeline.
* **Context tooltips** on sliders and provider selectors (e.g., *Fast pacing ≈ 190 wpm*).
* **Theme:** Light/Dark auto; high contrast accessibility mode.
* **Status bar:** Non‑intrusive background tasks, cache usage, encoder in use.

### Main Navigation (left pane)

1. **Create** (wizard)
2. **Storyboard** (edit & preview)
3. **Render** (export)
4. **Library** (past projects)
5. **Settings** (keys, providers, brand kit)

### Create Wizard (zero‑to‑video in minutes)

* **Step 1 – Brief**

  * **TextArea:** *“What’s your video about?”* (niche/topic/goal/target audience).
  * **Tone dropdown:** Informative, Narrative, Humorous, Dramatic, Educational, Listicle, Documentary, Commentary.
  * **Language dropdown** (default auto‑detect English).
  * **Aspect ratio:** 16:9, 9:16, 1:1.
* **Step 2 – Duration & Pacing**

  * **Length slider:** 30 sec ↔ 20 min (default 6 min). Shows estimated word count.
  * **Pacing slider:** Chill ↔ Conversational ↔ Fast (maps to WPM + pause lengths).
  * **Density slider:** Sparse ↔ Balanced ↔ Dense (how info‑heavy each segment is).
* **Step 3 – Voice & Music**

  * **Voice:** Windows local voices (free) + optional Pro TTS (ElevenLabs/PlayHT via key).
  * **Speech rate** (0.8–1.3x), **pitch** (‑3 to +3 semitones), **pause style** (breath, sentence gap).
  * **Music:** Free stock library fetch (YouTube Audio Library local index) + volume ducking.
* **Step 4 – Visuals**

  * **Style presets:** B‑roll‑first, Infographic, Slideshow, Mixed.
  * **B‑roll source toggles:** Local folder, Pixabay, Pexels, Unsplash (free keys supported), plus Stable Diffusion (local WebUI) if detected. Pro: Runway/Stability/GEN‑3 via key.
  * **Brand kit:** Colors, fonts, watermark, intro/outro bumper toggle.
* **Step 5 – Providers**

  * **Mode selector:** **Free Mode** (no keys) or **Pro Mode** (any keys added).
  * Show detected **Ollama** (local LLM) and **SD WebUI** if installed; otherwise provide prompts to install.
* **Step 6 – Confirm**

  * Summary of plan: outline, estimated duration, asset counts, providers chosen.
  * **Buttons:** *Quick Generate (Free)* and *Generate with Pro*.

### Storyboard View

* **Premiere‑style timeline**, simplified:

  * **Tracks:** V1 (visuals), V2 (overlays/text/lower thirds), A1 (narration), A2 (music/SFX). Add more if needed.
  * **Editing tools:** Split (S), Ripple trim (Q/W), Slip/Slide, Roll, Nudge (← →), Snapping (Shift+S), Magnetic timeline option.
  * **Markers:** Scene markers, Beat markers (auto‑detect from music), Chapter markers (export to YouTube chapters).
  * **Transitions:** Crossfade, Dip to Black/White, Push/Slide, Whip‑Pan, Zoom. Drag‑drop from a **Transitions Palette**.
  * **Effects per clip:** Ken Burns, Scale, Position, Opacity, Blur, Sharpen, Speed (0.5×–2×), Reverse. Keyframes with simple ease presets.
  * **Color panel (per scene/clip):** Exposure, Contrast, Saturation, Temp/Tint, Vibrance; **LUT** slot (Cube); Vectorscope preview (simplified) and Histogram.
  * **Text & Graphics:** Title, Subtitle, Lower third, Callout arrows/boxes, Progress bar; Safe‑areas overlay.
  * **Audio lanes:** Clip gain, Pan, Solo/Mute, Ducking visualization, Waveform zoom.
  * **Preview quality:** Auto/Full/Half/Quarter; **Proxy toggle** (auto‑generated for 4K/RAW sources).
* **Inspector (right):** Contextual controls for the selected clip/transition with **Basic** and **Advanced** tabs.
* **Library (left):** Scenes list + assets bin with search, favorites, license badges, and usage counts.

### Render View

* **Presets:** YouTube 1080p, YouTube Shorts (9:16 1080×1920), 1440p, 4K; **HDR10** (Tier A only, optional).
* **Resolution:** 720p, 1080p, 1440p, 2160p; **Scaling**: Auto, Lanczos, Bicubic.
* **Framerate:** 23.976, 24, 25, 29.97, 30, 50, 59.94, 60 (and 120 for Tier A preview experiments). CFR default; VFR optional.
* **Codec:** H.264/AVC (baseline/main/high), HEVC/H.265 (Main/Main10), **AV1** (RTX 40/50 only). Container MP4/MKV/MOV.
* **Quality controls:**

  * **Quality vs Speed slider** mapping to encoder params (documented):

    * x264: CRF 28→14, preset veryfast→slow, tune film.
    * NVENC H.264/HEVC: `rc=cq`, `cq=33→18`, `preset=p5→p7`, `rc-lookahead=16`, `spatial-aq=1`, `temporal-aq=1` (Tier‑aware caps).
    * AV1 NVENC (if available): `cq=38→22`, `preset=p5→p7`.
  * **Bitrate mode:** Auto (CQ/CRF), Target (1‑pass), or 2‑Pass (x264 only) with target bitrate.
  * **GOP/Keyframe interval:** Auto (2×fps) or custom; B‑frames (0–4), CABAC toggle when applicable.
* **Color & Range:** BT.709 (default), BT.2020 (HDR10); Full/Video range; tone‑mapping toggle for HDR assets.
* **Audio:**

  * **Codec:** AAC‑LC (default), Opus (MKV), PCM WAV (master).
  * **Sample rate:** 44.1 or **48 kHz** (default). **Bit depth:** 16‑bit or 24‑bit (WAV master).
  * **Channels:** Mono, **Stereo** (default).
  * **Loudness target:** −16 LUFS (voice‑only), **−14 LUFS** (YouTube default), −12 LUFS (music‑forward). Peak ceiling −1 dBFS.
  * **Dynamics:** De‑esser, Noise Gate, High‑pass, **Compressor** (ratio/attack/release), Music ducking depth & release.
* **Subtitles & Captions:** Burn‑in or sidecar SRT/VTT; styling (font, size, outline, background).
* **Render Queue:** Add multiple outputs; queue shows ETA, hardware in use (NVENC/x264), and logs.
* **Smart Cache:** prerender complex sections; reuse cache across exports if timeline unchanged.

### Settings

* API keys vault (secure local DPAPI encryption): OpenAI, Azure OpenAI, Google Gemini, ElevenLabs, PlayHT, Stability, Runway.
* Free sources: Pixabay/Pexels/Unsplash keys (optional), or purely local folder.
* Brand kit, default language, cache size, telemetry (off by default), GPU detection for SD.

### Accessibility & Intl

* High contrast theme, font scaling, keyboard nav, ARIA names. Right‑to‑left support.

---

## Hardware‑Aware Free Path & Offline Setup (new)

**Objective:** Make the *Free Mode* fully offline and *automatically tailored* to the user’s machine, with a simple first‑run experience that detects hardware, proposes a compatible pipeline, and (optionally) downloads everything needed.

### First‑Run Hardware Wizard

* **Auto‑Detect** using WMI + lightweight probes:

  * CPU: name, physical cores, logical processors, base/max clocks (`Win32_Processor`).
  * RAM: total/available (`Win32_OperatingSystem.TotalVisibleMemorySize`).
  * GPU(s): vendor, model, VRAM, driver version (`Win32_VideoController`). If NVIDIA is present and `nvidia-smi.exe` exists, parse `nvidia-smi -q -x` for precise VRAM and NVENC codec support. Fallback to `ffmpeg -hwaccels` to enumerate hardware encoders.
  * Disk: free space on selected library/cache/render paths.
* **Manual Override** (always available): drop‑downs for **System RAM** (8–256 GB), **CPU** (core count 2–32+), **GPU** series with presets:

  * NVIDIA: **50‑series**, **40‑series**, **30‑series** (e.g., *RTX 3080 10GB*), **20‑series**, **16‑series**, **10‑series** (1060/1070/1080).
  * AMD: RX 7000 / 6000 / 5000.
  * Intel: Arc A‑series, Intel iGPU (QSV capable).
  * Each preset shows *VRAM*, *NVENC/AMF/QSV* capabilities, and recommended settings.

### Capability Tiers (maps hardware → defaults)

* **Tier A (High)**: ≥12GB VRAM or NVIDIA 40/50‑series → SDXL local allowed; 4K export presets; HEVC NVENC by default; parallel image synthesis; preview at 1440p.
* **Tier B (Upper‑mid)**: 8–12GB VRAM (e.g., **RTX 3080 10GB**, 3070, 2080 Ti) → SDXL (reduced batch) or SD 1.5 fast; 1080p/1440p export; HEVC/H.264 NVENC.
* **Tier C (Mid)**: 6–8GB VRAM (e.g., 2060/2070, 1660 Ti) → SD 1.5 only; 1080p export; H.264 NVENC/AMF/QSV.
* **Tier D (Entry)**: ≤4–6GB VRAM or no GPU → No diffusion by default (slides + stock only); 720p/1080p software x264; conservative effects.

> The wizard **always explains** the chosen tier and lets users toggle features (e.g., disable diffusion to save time).

### Pipeline Configuration from Profile

* **Composer:** choose hardware encoder (NVENC/AMF/QSV) or x264. Validate with a 2‑second **NVENC Probe** (tiny render) and auto‑fallback if it fails.
* **LLM (Free):**

  * If **Ollama** is detected, use it; else **RuleBasedLlmProvider** (deterministic templates).
  * Offer **Install Ollama** toggle.
* **Image Gen (Free):**

  * **NVIDIA‑only local diffusion policy** (see below). If VRAM ≥8GB and NVIDIA is present with user opt‑in, enable **Stable Diffusion WebUI** (SDXL for Tier A/B, SD 1.5 otherwise). If not present, offer **Install SD WebUI**.
  * Otherwise, default to slideshow/stock visuals.
* **TTS:** Windows SAPI voices always available.
* **Caching:** set concurrency (threads), memory caps, and disk cache sizes from profile.

### Local Generation Policy (NVIDIA‑only)

* Local diffusion (SD WebUI/ComfyUI) is **enabled only when an NVIDIA GPU** is detected with sufficient VRAM and compatible drivers.
* On AMD/Intel systems, local diffusion controls are disabled with a tooltip explaining the policy and offering **Pro** cloud options or stock/slideshow fallback.
* Driver/VRAM checks enforce SDXL (≥12GB) vs SD 1.5 (≥6–8GB). Below thresholds → diffusion off.

### Download Center (optional, one click)

Unified downloader with checksum verification, resume, and progress bars. Items:

* **FFmpeg (portable)** → required (bundled by default; repair if missing).
* **Ollama (Windows)** + model suggestions: `llama3.1:8b Q4_K_M` (≈4–5GB download), or `mistral:7b-instruct` (≈4GB). User can skip.
* **Stable Diffusion WebUI (portable)** or **ComfyUI portable** with:

  * **SD 1.5** (safetensors), **SDXL** (Tier A/B only), **VAE**, optional **ControlNet** packs.
* **Free B‑roll pack** (CC0 stock thumbnails/videos) and **Music/SFX pack** (CC0) for fully offline demo.

Each item shows size, disk space required, and a **Destination** picker. Downloader validates SHA‑256, retries transient failures, and logs.

### Dependency Manager (new)

* Central service to orchestrate downloads/installs, path validation, environment checks (x64, AV exceptions opt‑in), and version pinning.
* Maintains a **manifest.json** with component versions and checksums for reproducible setups.
* Exposes a **Dependency Status** panel (OK/Warning/Missing) with one‑click *Repair*.

### Preflight & Self‑Test (bug checking)

* **Render Probe:** 5s slideshow with captions → ensures FFmpeg & fonts OK.
* **TTS Probe:** synthesize 3 lines and play back.
* **NVENC/AMF/QSV Probe:** tiny encode test; auto‑fallback to software x264 on error.
* **SD Probe:** 1 test image at low steps; disabled if VRAM too low.
* **Disk Space Check:** warn if <10 GB free in renders/cache.
* **Driver Check (NVIDIA):** read `nvidia-smi` driver version; warn if older than feature baseline for NVENC HEVC (show link to update).

### UX Additions

* **Settings → System Profile** card with **Auto‑Detect** button, current tier, and **Edit** (manual overrides).
* **Create Wizard – Step 0 (First‑Run only):** *“Pick your performance profile”* with preview of estimated render time for the chosen length.
* **One‑toggle Offline Mode:** forces stock/slides, disables network fetch, and uses only local assets/providers.

### Safety & Fallbacks

* Timeouts and circuit‑breakers around probes and downloads.
* Every failure produces a human‑readable explanation and a *Fix* button (e.g., “Switch to software encoder”).
* Profile changes ripple to the storyboard/time estimates immediately.

---

## Core Capabilities (must‑have)

1. **Planning & Scripting**

   * Generate outline and script from the brief, length, pacing, tone, density.
   * **Free Path:**

     * If **Ollama** (Windows) is installed, use `llama3.1:8b` or `mistral` (local, no key) via HTTP.
     * Else use a rule‑based template engine that expands the outline using curated prompts and deterministic pattern libraries (ensures zero‑key operation).
   * **Pro Path:** Use LLM via **OpenAI** (e.g., GPT‑4o‑mini or latest), or Azure OpenAI, or Google Gemini when keys present.
2. **Narration**

   * **Free Path:** Windows 11 **SAPI** voices (or `Windows.Media.SpeechSynthesis`).
   * **Pro Path:** ElevenLabs / PlayHT TTS.
   * Auto‑segment script into scenes, compute timecodes using WPM and pacing.
3. **B‑roll & Visuals**

   * Fetch images/clips matching each scene keyword; prefer free stock (Pixabay/Pexels/Unsplash) with attribution metadata.
   * Optional local **Stable Diffusion** (if SD WebUI at `http://127.0.0.1:7860` detected) to generate scene art.
   * Ken Burns, pan/zoom, crossfades, lower thirds, progress bar overlay.
4. **Music & SFX**

   * Free music (local pack) with BPM detection; duck under speech with sidechain‑like envelope (NAudio).
5. **Subtitles & Chapters**

   * Build SRT/VTT from the script timecodes (no ASR needed). Auto chapters from headings.
6. **Thumbnail**

   * Generate via SkiaSharp: background image, large title text, stroke/outline, brand colors, face cutout if detected.
7. **Rendering**

   * Compose timeline → render with FFmpeg (concat filter, overlays, subtitles, audio mix). Progress UI with estimates.
8. **Metadata & Upload** (optional)

   * YouTube Data API: scopes, OAuth, title/desc/tags/chapters/thumbnail, privacy, schedule.
9. **Projects & Caching**

   * Project folder with `project.json`, `assets/`, `renders/`, reproducible pipeline log.

---

## Provider Abstraction

Create interfaces with clean DI so free/pro can be swapped.

```csharp
public interface ILlmProvider { Task<string> DraftScriptAsync(Brief brief, PlanSpec spec, CancellationToken ct); }
public interface ITtsProvider { Task<string> SynthesizeAsync(IEnumerable<ScriptLine> lines, VoiceSpec spec, CancellationToken ct); }
public interface IImageProvider { Task<IReadOnlyList<Asset>> FetchOrGenerateAsync(Scene scene, VisualSpec spec, CancellationToken ct); }
public interface IVideoComposer { Task<string> RenderAsync(Timeline timeline, RenderSpec spec, IProgress<RenderProgress> progress, CancellationToken ct); }
public interface IStockProvider { Task<IReadOnlyList<Asset>> SearchAsync(string query, int count, CancellationToken ct); }
```

### Hybrid Pipeline & Provider Mixing (new)

**Goal:** Allow a mixed free/pro pipeline per stage with explicit user control and safe fallbacks.

| Stage               | Free (Local/No‑Key)                      | Pro (API Key)              | Mixing Rules                                                                          |
| ------------------- | ---------------------------------------- | -------------------------- | ------------------------------------------------------------------------------------- |
| **Scripting**       | RuleBased or Ollama                      | OpenAI/Azure/Gemini        | Prefer Pro if key present; fallback to Free on error.                                 |
| **Narration**       | Windows SAPI                             | ElevenLabs / PlayHT        | Scene‑level override allowed (per‑scene voice).                                       |
| **Visuals**         | Stock/Slides; **Local SD (NVIDIA‑only)** | Stability/Runway/Pro Stock | If local SD disabled (non‑NVIDIA/low VRAM), auto use stock; Pro only if user opts‑in. |
| **Compose/Render**  | FFmpeg (NVENC/AMF/QSV/x264)              | —                          | Always local; pick best encoder from probes.                                          |
| **Metadata/Upload** | Local generation of SRT/chapters         | YouTube Data API           | Upload optional; never auto without consent.                                          |

* The **Storyboard Inspector** exposes per‑stage provider dropdowns (e.g., Scripting: Free/Pro; TTS: Windows/ElevenLabs) with tooltips and *Reset to Recommended*.
* Orchestrator logs the chosen provider for every stage; on failure it logs and **downgrades** to the nearest free/local alternative automatically.
* **Profile presets** (Free‑Only, Balanced Mix, Pro‑Max) preselect mixing per stage and can be saved.
  csharp
  public interface ILlmProvider { Task<string> DraftScriptAsync(Brief brief, PlanSpec spec, CancellationToken ct); }
  public interface ITtsProvider { Task<string> SynthesizeAsync(IEnumerable<ScriptLine> lines, VoiceSpec spec, CancellationToken ct); }
  public interface IImageProvider { Task<IReadOnlyList<Asset>> FetchOrGenerateAsync(Scene scene, VisualSpec spec, CancellationToken ct); }
  public interface IVideoComposer { Task<string> RenderAsync(Timeline timeline, RenderSpec spec, IProgress<RenderProgress> progress, CancellationToken ct); }
  public interface IStockProvider { Task<IReadOnlyList<Asset>> SearchAsync(string query, int count, CancellationToken ct); }

````

### Free Implementations (no paid keys)
- `RuleBasedLlmProvider` (templated expander with heuristics + prompt snippets).
- `OllamaLlmProvider` (if detected at `http://localhost:11434`).
- `WindowsTtsProvider` (SAPI).
- `LocalStockProvider` (user folder) + `PixabayProvider` (free tier key) + `PexelsProvider` + `UnsplashProvider` (all optional keys; if none, still work with local assets and generated slides).
- `StableDiffusionWebUiProvider` (optional, local server if present).
- `FfmpegVideoComposer` (Ken Burns/text/overlays).

### Pro Implementations (keys required to unlock)
- `OpenAiLlmProvider`, `AzureOpenAiLlmProvider`, `GeminiLlmProvider`.
- `ElevenLabsTtsProvider`, `PlayHttTtsProvider`.
- `StabilityImageProvider`, `RunwayVideoProvider` (if API available).

> The app **must run fully without any keys** using the free implementations.

---

## Intelligent Planning Logic
- Compute **total words** from (length slider + pacing): `targetWords = minutes * wpm[pacing] * densityFactor`.
- Auto derive **scene count** (e.g., 6–12 per 6‑min video) and **words per scene**.
- Recommend **asset count** per scene. If insufficient stock matches, fallback to **title card + text motion**.
- Generate **B‑roll queries** from script nouns/entities.
- Voice timing from word count + punctuation pause table (.,!?;): base 250ms per comma, 600ms per period (adjust by pacing).

---

## Data Models (simplified)
```csharp
record Brief(string Topic, string? Audience, string? Goal, string Tone, string Language, Aspect Aspect);
record PlanSpec(TimeSpan TargetDuration, Pacing Pacing, Density Density, string Style);
record VoiceSpec(string VoiceName, double Rate, double Pitch, PauseStyle Pause);
record Scene(int Index, string Heading, string Script, TimeSpan Start, TimeSpan Duration);
record ScriptLine(int SceneIndex, string Text, TimeSpan Start, TimeSpan Duration);
record Asset(string Kind, string PathOrUrl, string? License, string? Attribution);
record RenderSpec(Resolution Res, string Container, int VideoBitrateK, int AudioBitrateK);
````

---

## FFmpeg Composition (reference)

* Build temp `concat.txt` for clips; `zoompan`/`scale` for stills; `overlay` for text/graphics; `subtitles` for SRT burn‑in; `drawbox/drawtext` for bars and captions.
* **Encoder mappings:**

  * **x264:** `-crf <14–28> -preset <slow…veryfast> -tune film -profile:v high -pix_fmt yuv420p`.
  * **NVENC H.264/HEVC:** `-rc cq -cq <18–33> -preset <p7…p5> -rc-lookahead 16 -spatial-aq 1 -temporal-aq 1 -bf 3` plus `-profile:v high|main10`.
  * **AV1 (NVENC 40/50):** `-rc cq -cq <22–38> -preset <p7…p5>`.
* **Framerate:** enforce CFR with `-r` and set GOP `-g <2×fps>`; insert scene‑cut keyframes.
* **Audio chain (NAudio → FFmpeg):** HPF → De‑esser → Compressor → Limiter; export WAV 48k/24‑bit, then encode to AAC/Opus; measure LUFS and normalize.
* **Color:** tag color primaries/transfer (BT.709/BT.2020), matrix flags; optional HDR10 metadata when enabled.
* **Validation:** unit‑test the builder to ensure flags reflect chosen UI options; refuse illegal combos (e.g., AV1 on non‑supported GPUs).

---

## Security, Privacy, and Offline

* Store API keys encrypted with DPAPI (`ProtectedData`), never plain text.
* Allow complete offline operation (free path). If online stock/LLM fails, degrade gracefully.
* Explicit license capture for each asset and store alongside the project.

---

## Error Handling & UX Feedback

* Use `InfoBar` and toast notifications for recoverable issues.
* Progress page shows per‑task status (Scripting, TTS, B‑roll, Music, Render) with retry.
* Collect a zip of logs/assets for quick bug report.

---

## Settings Schema (`appsettings.json`)

````json
{
  "Providers": {
    "Mode": "Free",
    "LLM": { "Kind": "RuleBased|Ollama|OpenAI|Azure|Gemini", "BaseUrl": "", "Model": "" },
    "TTS": { "Kind": "Windows|ElevenLabs|PlayHT", "Voice": "", "Rate": 1.0, "Pitch": 0 },
    "Images": { "PixabayKey": "", "PexelsKey": "", "UnsplashKey": "", "StableDiffusionUrl": "http://127.0.0.1:7860" },
    "Video": { "FfmpegPath": "scripts/ffmpeg/ffmpeg.exe" }
  },
  "Hardware": {
    "Detection": { "Auto": true, "LastRun": null, "UseNvidiaSmi": true },
    "CPU": { "Logical": 16, "Physical": 8 },
    "RAMGB": 32,
    "GPU": { "Vendor": "NVIDIA", "Model": "RTX 3080", "VRAMGB": 10, "Series": "30" },
    "Tier": "B",
    "Overrides": { "EnableNVENC": true, "EnableSD": true, "OfflineOnly": false }
  },
  "Downloads": {
    "AutoOfferOnFirstRun": true,
    "Targets": {
      "FFmpeg": { "Required": true, "Installed": true },
      "Ollama": { "Offer": true, "Install": false, "Model": "llama3.1:8b-q4_k_m" },
      "StableDiffusion": { "Offer": true, "Install": false, "Variant": "SDXL|SD15", "NvidiaOnly": true },
      "Packs": { "Stock": true, "Music": true }
    },
    "Locations": { "Downloads": "C:/Aura/Downloads", "Cache": "C:/Aura/Cache", "Projects": "C:/Aura/Projects" }
  },
  "Profiles": {
    "Active": "Balanced Mix",
    "Saved": [
      { "Name": "Free‑Only", "Stages": { "Script": "Free", "TTS": "Windows", "Visuals": "Stock", "Upload": "Off" } },
      { "Name": "Balanced Mix", "Stages": { "Script": "ProIfAvailable", "TTS": "Windows", "Visuals": "StockOrLocal", "Upload": "Ask" } },
      { "Name": "Pro‑Max", "Stages": { "Script": "Pro", "TTS": "Pro", "Visuals": "Pro", "Upload": "Ask" } }
    ]
  },
  "Brand": { "Primary": "#6750A4", "Secondary": "#03DAC6", "Font": "Segoe UI" },
  "Render": { "Preset": "YouTube1080p", "BitrateK": 12000, "AudioBitrateK": 256 }
}
```json
{
  "Providers": {
    "Mode": "Free", // or "Pro"
    "LLM": { "Kind": "RuleBased|Ollama|OpenAI|Azure|Gemini", "BaseUrl": "", "Model": "" },
    "TTS": { "Kind": "Windows|ElevenLabs|PlayHT", "Voice": "", "Rate": 1.0, "Pitch": 0 },
    "Images": { "PixabayKey": "", "PexelsKey": "", "UnsplashKey": "", "StableDiffusionUrl": "http://127.0.0.1:7860" },
    "Video": { "FfmpegPath": "scripts/ffmpeg/ffmpeg.exe" }
  },
  "Hardware": {
    "Detection": { "Auto": true, "LastRun": null, "UseNvidiaSmi": true },
    "CPU": { "Logical": 16, "Physical": 8 },
    "RAMGB": 32,
    "GPU": { "Vendor": "NVIDIA", "Model": "RTX 3080", "VRAMGB": 10, "Series": "30" },
    "Tier": "B",
    "Overrides": { "EnableNVENC": true, "EnableSD": true, "OfflineOnly": false }
  },
  "Downloads": {
    "AutoOfferOnFirstRun": true,
    "Targets": {
      "FFmpeg": { "Required": true, "Installed": true },
      "Ollama": { "Offer": true, "Install": false, "Model": "llama3.1:8b-q4_k_m" },
      "StableDiffusion": { "Offer": true, "Install": false, "Variant": "SDXL|SD15" },
      "Packs": { "Stock": true, "Music": true }
    },
    "Locations": { "Downloads": "C:/Aura/Downloads", "Cache": "C:/Aura/Cache", "Projects": "C:/Aura/Projects" }
  },
  "Brand": { "Primary": "#6750A4", "Secondary": "#03DAC6", "Font": "Segoe UI" },
  "Render": { "Preset": "YouTube1080p", "BitrateK": 12000, "AudioBitrateK": 256 }
}
``` (`appsettings.json`)
```json
{
  "Providers": {
    "Mode": "Free", // or "Pro"
    "LLM": { "Kind": "RuleBased|Ollama|OpenAI|Azure|Gemini", "BaseUrl": "", "Model": "" },
    "TTS": { "Kind": "Windows|ElevenLabs|PlayHT", "Voice": "", "Rate": 1.0, "Pitch": 0 },
    "Images": { "PixabayKey": "", "PexelsKey": "", "UnsplashKey": "", "StableDiffusionUrl": "http://127.0.0.1:7860" },
    "Video": { "FfmpegPath": "scripts/ffmpeg/ffmpeg.exe" }
  },
  "Brand": { "Primary": "#6750A4", "Secondary": "#03DAC6", "Font": "Segoe UI" },
  "Render": { "Preset": "YouTube1080p", "BitrateK": 12000, "AudioBitrateK": 256 }
}
````

---

## Non‑AI Free Behaviors (no key, no model required)

* Script generation via deterministic templates + pattern libraries:

  * Hook ➜ Promise ➜ Body sections (N) ➜ Recap ➜ CTA.
  * Fillers vary by tone (humor/educational/etc.).
  * Build facts from the user brief and generic knowledge disclaimers; never claim factual specificity without an LLM.
* Visuals fallback: text‑over‑background slides with gradient and iconography (Fluent Emojis, local pack).
* Music: bundled CC0 tracks; pick by tempo nearest to pacing.

---

## Pro Behaviors (when key present)

* LLM drafts: topic research, outlines, script, alternative takes, SEO‑keywords.
* TTS with neural voices; pronunciation lexicon import.
* Image/video gen via provider; upscale if needed.

---

## File/Folder Layout (expected)

```
Aura.sln
  /Aura.App
    App.xaml, App.xaml.cs
    MainWindow.xaml(.cs)
    Views/ (CreateView.xaml, StoryboardView.xaml, RenderView.xaml, SettingsView.xaml)
    Controls/ (LengthSlider, PacingSlider, Timeline, SceneCard, VoiceControl)
    ViewModels/ (...)
    Assets/ (icons, placeholders)
  /Aura.Core
    Models/, Orchestrator/, Timeline/, Rendering/
  /Aura.Providers
    Llm/, Tts/, Stock/, ImageGen/, Video/
  /Aura.Tests
  /Aura.E2E
  /scripts/ffmpeg/ (ffmpeg.exe, ffprobe.exe)
  appsettings.json
```

---

## CI/CD

### GitHub Actions Workflow

The repository includes a comprehensive CI/CD workflow (`.github/workflows/ci.yml`) that runs on every push and pull request:

**Build and Test Job** (runs on `windows-latest`):
- Restores all NuGet dependencies
- Builds core projects: Aura.Core, Aura.Providers, Aura.Tests, Aura.E2E
- Runs all 84 unit tests with detailed reporting
- Runs all 8 E2E integration tests
- Uploads test results as artifacts

**Build WinUI App Job** (runs after tests pass):
- Builds the WinUI 3 desktop application (Aura.App)
- Creates MSIX package for Windows deployment
- Uploads MSIX artifact for distribution

### Running Locally

```bash
# Build the solution
dotnet build Aura.sln

# Run unit tests (84 tests)
dotnet test Aura.Tests/Aura.Tests.csproj

# Run E2E tests (8 tests)
dotnet test Aura.E2E/Aura.E2E.csproj

# Build for release
dotnet build Aura.sln --configuration Release
```

### Dependency Management

The `manifest.json` file contains all downloadable dependencies with SHA-256 checksums:
- **FFmpeg 6.0** (required) - Video processing binaries
- **Ollama** (optional) - Local LLM runtime
- **Stable Diffusion WebUI** (optional, NVIDIA-only) - Local image generation
- **CC0 Asset Packs** (optional) - Free stock images and music

All downloads are verified using SHA-256 checksums before installation.

---

## Acceptance Criteria (must‑have)

1. **Zero‑Key Run:** First‑run **Hardware Wizard** (auto or manual) → **Quick Generate** outputs 1080p MP4 with narration, music ducking, captions, slideshow/stock. No keys.
2. **Hybrid Mixing:** Users can combine local/free and pro providers **per stage** and save/load profiles. On any failure, app **downgrades** gracefully and logs the decision.
3. **NVIDIA‑Only Local Diffusion:** Local SD only when NVIDIA detected and VRAM threshold met; AMD/Intel show disabled control with explanatory tooltip and stock/Pro alternatives.
4. **Download/Dependency Manager:** Accurate sizes, SHA‑256 verification, resume, and *Repair* actions. Skippable yet still functional in Free‑Only profile.
5. **UX Quality:** Resizable panes, tooltips, inline notes, status bar, accessible themes, and clear errors with copyable logs.
6. **Windows 11 x64 Reliability:** All probes (Render/TTS/NVENC/SD/Disk) pass or produce actionable fallbacks. App runs without admin rights and writes to `%LOCALAPPDATA%` by default.
7. **Render:** Hardware encoder selected by probes (NVENC/AMF/QSV) or x264 fallback; output loudness ≈ ‑14 LUFS.
8. **Data & Persistence:** Profiles, brand kit, hardware profile, and project settings are saved and restorable. Import/Export profile JSON works.
9. **Tests:** Unit (tiering, filtergraphs, rule‑based scripts, TTS envelope), Integration (probes), E2E (offline demo render), plus CI on Windows latest.

## Detailed Tasks for Copilot (implement in order)

1. **Solution scaffolding** … (keep existing items) …
2. **Hardware Module** – detection, tiering, NVIDIA policy enforcement, probes.
3. **Provider Mixing UI** – per‑stage selectors, profile save/load, Reset to Recommended.
4. **Download/Dependency Manager** – manifest, SHA‑256, resume, repair.
5. **Diagnostics & Logging** – Serilog rolling files, in‑app Log Viewer, crash dumps opt‑in.
6. **Orchestrator** – stage selection, downgrade logic, structured logging of decisions.
7. **UX polish** – resizable panes, tooltips, status bar, accessibility.
8. **Tests & CI** – mocks/fixtures; golden tests; GitHub Actions Windows runner.
9. **Docs & MSIX** – first‑run guide; troubleshooting; offline/pro mixing examples.

(implement in order)

1. **Solution scaffolding** … (keep existing items) …
2. **Hardware Module**

   * WMI detection + `nvidia-smi`/`ffmpeg -hwaccels` probes.
   * Tiering logic and preset maps (series → VRAM expectations → defaults).
   * Manual override UI and persistence.
3. **Download Center**

   * Multi‑file downloads with SHA‑256, resume, progress, and cancel; destination pickers.
   * Install helpers for Ollama and SD WebUI portable.
4. **Probes & Preflight**

   * Render/TTS/NVENC/SD probes with timeouts and actionable failures.
5. **Orchestrator integration**

   * Use profile to set encoder, diffusion availability, concurrency, cache sizes.
6. **UI**

   * First‑Run Hardware Wizard (Step 0), Settings → System Profile card, and Offline Mode toggle.
7. **Tests & CI**

   * Mock WMI + fixture JSON for `nvidia-smi` output.
   * Golden tests for tier decisions and FFmpeg filtergraphs.
8. **Docs & MSIX**

   * First‑run guide explaining tiers and offline/pro options.

(implement in order)

1. **Solution scaffolding** with projects, DI host, appsettings loader, logging (Serilog).
2. **Models & Enums** for Brief, PlanSpec, VoiceSpec, Scene, ScriptLine, Asset, RenderSpec, Pacing, Density, Aspect, etc.
3. **RuleBasedLlmProvider**

   * Deterministic templates per tone.
   * Length/pacing ➜ word budget ➜ scene allocation.
4. **WindowsTtsProvider**

   * Enumerate Windows voices; synthesize per scene to WAV; stitch; generate envelope for ducking.
5. **Local/Stock Providers**

   * Local folder scan; Pixabay/Pexels/Unsplash minimal clients with attribution fields.
6. **FfmpegPlanBuilder**

   * Generate filtergraph for images/video with pan/zoom, text overlays, watermark, subtitles.
   * Unit tests verifying filtergraph text given inputs.
7. **Audio Mixer** (NAudio)

   * Loudness normalization; build sidechain envelope to auto‑duck music under narration.
8. **Storyboard UI**

   * Scene list, preview, inspector; draggable order; editable timings with ripple.
9. **Render Pipeline**

   * Orchestrator that runs: Plan → Script → TTS → Assets → Compose → Render → Thumbnail → Captions.
   * Progress reporting; cancellation.
10. **Pro Providers** (behind keys)

    * OpenAI (chat completions), Azure OpenAI, Gemini; ElevenLabs/PlayHT; SD WebUI; Stability/Runway (if keys exist).
11. **YouTube Upload** (optional)

    * OAuth flow, video upload with metadata; handle rate limits.
12. **Settings UI** and secure key storage (DPAPI).
13. **MSIX Packaging** and first‑run experience with sample project.

---

## UI Components (must include)

* **Hardware Profile Wizard** (first‑run) + **Offline Mode** toggle.
* **Provider Mixing Controls** per stage; **Profiles** save/load/import/export.
* **Premiere‑style Timeline**: multi‑track, ripple/roll/slip/slide, snapping, markers, transitions palette, keyframes, proxy toggle, preview quality selector.
* **Inspector**: Basic/Advanced tabs for clip properties (transform, speed, effects, color), transition tuning, and text styling.
* **Render Settings**: resolution, framerate, codec, quality vs speed, bitrate/CRF/CQ, color space, audio codec/rate/bit‑depth/channels, loudness target, subtitles.
* **Audio Mixer**: per‑track gain/pan, master loudness meter (LUFS/peak), compressor/ducking controls.
* **Download Center / Dependency Status**: progress, checksums, Repair.
* **Status bar**: encoder in use, task progress, cache usage.
* **InfoBar & Log Viewer**: errors, warnings, copyable diagnostics.

## Testing Guidance (write tests!) (write tests!) (write tests!) (write tests!)

* **Unit:**

  * Planning math (words, scenes) exact values.
  * Filtergraph strings compare to golden files.
  * RuleBased template selection deterministic for given seed.
* **E2E Smoke:** Use the free path with a static brief, render 10–15 sec, assert MP4 exists and duration tolerance ±0.5s.
* **Performance:** A 6‑min slideshow video completes under 5 minutes on average modern hardware (assume i5/Ryzen w/ iGPU). (Provide a perf test that measures from orchestration start to MP4 existence, skipping network.)

---

## Copywriting Prompts (internal)

When LLM is available, use high‑quality prompts with structure (system + user) to draft outline and script matching tone and density. Ensure attribution notes if specific facts are included. Otherwise, use deterministic templates.

---

## Guardrails

* Never upload or call external services without explicit user opt‑in.
* License/attribution recorded for every fetched asset.
* API failures must not block the Free Path.
* Clear disclaimer when facts are LLM‑generated; encourage human review.

---

## Deliverables

* Complete WinUI 3 solution with the above structure and providers.
* MSIX package and README with one‑click Quick Generate demo.
* Automated tests and CI workflow.

**Now implement the full application end‑to‑end following this specification. Prioritize stability, clarity, and maintainability.**
