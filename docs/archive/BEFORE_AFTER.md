# Before & After: The wwwroot Fix

## The Problem

Users were getting a 404 error when trying to access the web UI because the `wwwroot` directory was not in the correct location.

## Before (Broken) ❌

### Directory Structure
```
artifacts/windows/portable/build/
├── Api/
│   └── Aura.Api.exe
├── wwwroot/              ← Wrong location!
│   ├── index.html
│   └── assets/
├── ffmpeg/
├── Launch.bat
└── README.md
```

### Console Output
```
[23:13:42 WRN] wwwroot directory not found at: C:\...\build\wwwroot
[23:13:42 WRN] Static file serving is disabled. Web UI will not be available.
[23:13:42 INF] Now listening on: http://127.0.0.1:5005
[23:13:42 INF] Application started. Press Ctrl+C to shut down.
[23:13:43 INF] Request finished HTTP/1.1 GET http://127.0.0.1:5005/ - 404 0 null
```

### Browser
```
404 - Not Found
Request path: GET http://127.0.0.1:5005/
```

### Why It Failed
The API executable (`Aura.Api.exe`) looks for the `wwwroot` directory in its current working directory:
```csharp
var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
```

When running from `Api/Aura.Api.exe`, it looks for `Api/wwwroot/`, not `wwwroot/` at the parent level.

## After (Fixed) ✅

### Directory Structure
```
artifacts/windows/portable/build/
├── Api/
│   ├── Aura.Api.exe
│   └── wwwroot/          ← Correct location!
│       ├── index.html
│       └── assets/
├── ffmpeg/
├── Launch.bat
└── README.md
```

### Console Output
```
[04:27:45 INF] Serving static files from: /tmp/portable-test/Api/wwwroot ✅
[04:27:45 INF] Now listening on: http://127.0.0.1:5005
[04:27:45 INF] Application started. Press Ctrl+C to shut down.
[04:27:45 INF] Hosting environment: Production
```

### Browser
```html
<!doctype html>
<html lang="en">
  <head>
    <meta charset="UTF-8" />
    <title>Aura Video Studio</title>
    ...
  </head>
  <body>
    <div id="root"></div>
  </body>
</html>
```

### Why It Works
The `wwwroot` directory is now in the same directory as `Aura.Api.exe`, so the API can find it:
- Working Directory: `Api/`
- Executable: `Api/Aura.Api.exe`
- Static Files: `Api/wwwroot/` ✅

## The Fix in Build Scripts

### Old Build Script (Wrong)
```powershell
# Publish API
dotnet publish -o "artifacts/portable/build/Api"

# Copy Web UI to wrong location
Copy-Item "Aura.Web/dist/*" -Destination "artifacts/portable/build/wwwroot" ❌
```

### New Build Script (Correct)
```powershell
# Publish API
dotnet publish -o "artifacts/portable/build/Api"

# Create wwwroot INSIDE Api directory
$wwwrootDir = Join-Path "artifacts/portable/build/Api" "wwwroot"
New-Item -ItemType Directory -Force -Path $wwwrootDir

# Copy Web UI to correct location
Copy-Item "Aura.Web/dist/*" -Destination $wwwrootDir -Recurse ✅
```

## Visual Comparison

### File Path Analysis

**Before (Broken):**
```
When Aura.Api.exe runs:
├─ Current Directory:    C:\...\build\Api\
├─ Looking for wwwroot:  C:\...\build\Api\wwwroot\
└─ Actual location:      C:\...\build\wwwroot\        ← Mismatch! ❌
```

**After (Fixed):**
```
When Aura.Api.exe runs:
├─ Current Directory:    C:\...\build\Api\
├─ Looking for wwwroot:  C:\...\build\Api\wwwroot\
└─ Actual location:      C:\...\build\Api\wwwroot\   ← Match! ✅
```

## API Code Reference

Here's the relevant code from `Aura.Api/Program.cs`:

```csharp
// Serve static files from wwwroot (must be before routing)
var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
//                               ↑ This is "Api/" when running Aura.Api.exe
//                                              ↑ So it looks for "Api/wwwroot/"

if (Directory.Exists(wwwrootPath))
{
    Log.Information("Serving static files from: {Path}", wwwrootPath);
    app.UseDefaultFiles(); // Serve index.html as default file
    app.UseStaticFiles();
}
else
{
    Log.Warning("wwwroot directory not found at: {Path}", wwwrootPath);
    Log.Warning("Static file serving is disabled. Web UI will not be available.");
}
```

## How to Verify Your Build

After building, check the console output when starting the API:

### ✅ Success Indicators
- `[INF] Serving static files from: C:\...\Api\wwwroot`
- No warnings about "wwwroot directory not found"
- Browser loads the web UI without 404 errors

### ❌ Failure Indicators
- `[WRN] wwwroot directory not found at: ...`
- `[WRN] Static file serving is disabled`
- Browser shows 404 error

## Testing Your Build

```powershell
# 1. Build the portable version
.\scripts\packaging\build-portable.ps1

# 2. Extract the ZIP
Expand-Archive artifacts/portable/AuraVideoStudio_Portable_x64.zip -DestinationPath test/

# 3. Check the structure
tree test /F
# Should show: test/Api/wwwroot/index.html ✅

# 4. Run the application
cd test
.\Launch.bat

# 5. Check console for success message
# Should see: [INF] Serving static files from: ...wwwroot ✅
```

## Summary

| Aspect | Before | After |
|--------|--------|-------|
| **wwwroot Location** | `build/wwwroot/` ❌ | `build/Api/wwwroot/` ✅ |
| **API Console** | Warning: not found | Info: Serving files |
| **Browser** | 404 Error | Web UI loads |
| **Build Script** | Wrong copy path | Correct copy path |
| **User Experience** | Broken | Working |

## Key Takeaway

**The wwwroot directory MUST be in the same directory as Aura.Api.exe!**

This is because ASP.NET Core looks for static files relative to the current working directory, which is where the executable is located.
