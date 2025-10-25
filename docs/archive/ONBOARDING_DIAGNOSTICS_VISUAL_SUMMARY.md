# Onboarding Diagnostics + Path Pickers - Visual Summary

## New Components Created

### 1. InstallItemCard with Path Picker
```
┌─────────────────────────────────────────────────────────────┐
│  ✓  FFmpeg (Video encoding)  [Required]                    │
│                                                              │
│  Actions: [Install] [Use Existing] [Skip]                  │
└─────────────────────────────────────────────────────────────┘

When "Use Existing" is clicked:
┌─────────────────────────────────────────────────────────────┐
│  Use Existing FFmpeg (Video encoding)                       │
│                                                              │
│  Point Aura to your existing installation...                │
│                                                              │
│  Install Path (required) *                                  │
│  [C:\Tools\ffmpeg                                    ]       │
│  Full path to the installation directory                    │
│                                                              │
│  Executable Path (optional)                                 │
│  [C:\Tools\ffmpeg\bin\ffmpeg.exe                     ]       │
│  Path to the main executable                                │
│                                                              │
│  💡 After attaching, Aura will validate...                  │
│                                                              │
│  [Cancel] [Attach & Validate]                               │
└─────────────────────────────────────────────────────────────┘
```

### 2. File Locations Summary (After Successful Validation)
```
┌─────────────────────────────────────────────────────────────┐
│  📂 Where are my files?                                      │
│                                                              │
│  Here's where Aura stores your installed engines and        │
│  tools...                                                    │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  FFmpeg                                               │  │
│  │  📁 C:\Users\You\.aura\engines\ffmpeg                 │  │
│  │  [Open Folder] [Copy Path]                           │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Stable Diffusion WebUI                               │  │
│  │  📁 C:\Users\You\.aura\engines\stable-diffusion-webui │  │
│  │  [Open Folder] [Open Web UI] [Copy Path]             │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                              │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  💡 Tip: You can manage engines, add models, and configure  │
│  settings from the Downloads page...                        │
└─────────────────────────────────────────────────────────────┘
```

### 3. Download Diagnostics Dialog (For Failures)
```
┌─────────────────────────────────────────────────────────────┐
│  Download Failed - FFmpeg                                    │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  ⚠  [E-DL-404] File not found                        │  │
│  │                                                       │  │
│  │  The download file was not found at the expected     │  │
│  │  URL. This could mean the mirror is down...          │  │
│  │                                                       │  │
│  │  URL: https://example.com/ffmpeg.zip                 │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                              │
│  Available Solutions:                                        │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  📁 Use Existing Installation                         │  │
│  │  If you already have FFmpeg installed...             │  │
│  │  [Pick Existing Path...]                             │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  📄 Install from Local File                           │  │
│  │  If you've manually downloaded the archive...        │  │
│  │  Local File Path                                     │  │
│  │  [C:\Downloads\ffmpeg.zip                     ]      │  │
│  │  [Install from File]                                 │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  🔗 Use Custom Download URL                           │  │
│  │  If you have an alternative download source...       │  │
│  │  Custom URL                                          │  │
│  │  [https://mirror.example.com/ffmpeg.zip       ]      │  │
│  │  [Download from URL]                                 │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Try Another Mirror                                   │  │
│  │  Attempt to download from an alternative mirror...   │  │
│  │  [Try Another Mirror]                                │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                              │
│  [Close]                                                     │
└─────────────────────────────────────────────────────────────┘
```

## Onboarding Flow Changes

### Step 2: Install Required Components (NEW)

**Before:**
- Only showed "Install" button
- No way to use existing installations
- Blocked if download failed

**After:**
- Shows three options: [Install] [Use Existing] [Skip]
- Can attach existing installations with path validation
- Can skip optional components to use fallbacks
- Unblocks first-run even with offline mirrors

### Step 3: Validation & Demo (ENHANCED)

**Before:**
- Just showed "All Set!" on success
- No information about where files are

**After:**
- Shows "All Set!" success message
- Displays "📂 Where are my files?" section
- Lists all installed engines with exact paths
- Provides "Open Folder" and "Open Web UI" buttons
- Explains how to add models and access web interfaces

## Error Codes Explained

| Code | Meaning | Solutions Offered |
|------|---------|-------------------|
| E-DL-404 | File not found at URL | Pick existing, local file, custom URL, try mirror |
| E-DL-CHECKSUM | Downloaded file checksum mismatch | Pick existing, local file, try mirror |
| E-HEALTH-TIMEOUT | Engine failed to start/respond | Pick existing, check dependencies |
| E-DL-NETWORK | Network connection failed | Pick existing, local file, check firewall |

## Key Benefits

1. **Unblocks first-run**: Users can proceed even if downloads fail
2. **Works offline**: Can use existing installations without internet
3. **Transparent**: Shows exactly where files are stored
4. **Self-service**: Provides concrete fix actions for all error scenarios
5. **Educational**: Explains where to find files and how to add models
6. **Flexible**: Supports both fresh installs and existing installations

## Testing Coverage

- ✅ 12 Vitest unit tests (InstallItemCard component)
- ✅ 140 total tests passing
- ✅ Playwright E2E tests for:
  - Onboarding with existing FFmpeg path
  - Onboarding with existing SD install
  - Skipping optional components
  - Error handling for failed attachments
