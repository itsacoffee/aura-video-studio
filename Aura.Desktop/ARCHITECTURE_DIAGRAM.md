# Electron Desktop Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         Aura Desktop Application                         │
│                         (Electron Desktop App)                          │
└─────────────────────────────────────────────────────────────────────────┘
                                      │
                                      │ Entry Point
                                      ▼
                      ┌───────────────────────────────┐
                      │     electron/main.js          │
                      │    (Application Orchestrator)  │
                      │                               │
                      │  - Initializes all modules    │
                      │  - Manages lifecycle          │
                      │  - Coordinates startup        │
                      └───────────────┬───────────────┘
                                      │
                ┌─────────────────────┼─────────────────────┐
                │                     │                     │
                ▼                     ▼                     ▼
    ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
    │ WindowManager    │  │ BackendService   │  │  AppConfig       │
    │                  │  │                  │  │                  │
    │ - Create windows │  │ - Spawn backend  │  │ - Store config   │
    │ - Manage state   │  │ - Health checks  │  │ - Encryption     │
    │ - Show/hide      │  │ - Port detection │  │ - Persistence    │
    └──────────────────┘  └──────────────────┘  └──────────────────┘
                │                     │                     │
                │                     │                     │
                ▼                     ▼                     ▼
    ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
    │  TrayManager     │  │  MenuBuilder     │  │ ProtocolHandler  │
    │                  │  │                  │  │                  │
    │ - System tray    │  │ - App menu       │  │ - aura:// URLs   │
    │ - Notifications  │  │ - Context menu   │  │ - Deep linking   │
    │ - Quick actions  │  │ - Shortcuts      │  │ - File handling  │
    └──────────────────┘  └──────────────────┘  └──────────────────┘
                │
                │
                ▼
    ┌──────────────────────────────────────────────────────────┐
    │              WindowsSetupWizard                          │
    │                                                          │
    │  - First-run setup on Windows                           │
    │  - Compatibility checks                                 │
    │  - System configuration                                 │
    └──────────────────────────────────────────────────────────┘

┌───────────────────────────────────────────────────────────────────────┐
│                          IPC Communication                             │
│                 (Secure Bridge: Main ↔ Renderer)                      │
└───────────────────────────────────────────────────────────────────────┘
                                      │
                                      │ preload.js
                                      │ (contextBridge)
                                      │
                ┌─────────────────────┼─────────────────────┐
                │                     │                     │
                ▼                     ▼                     ▼
    ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
    │ ConfigHandler    │  │ SystemHandler    │  │  VideoHandler    │
    │                  │  │                  │  │                  │
    │ - get/set config │  │ - Dialogs        │  │ - Generation     │
    │ - Recent projects│  │ - Shell ops      │  │ - Progress       │
    │ - Secure storage │  │ - App info       │  │ - Status         │
    └──────────────────┘  └──────────────────┘  └──────────────────┘
                │                     │                     │
                │                     │                     │
                ▼                     ▼                     ▼
    ┌──────────────────┐  ┌──────────────────┐
    │ BackendHandler   │  │  FFmpegHandler   │
    │                  │  │                  │
    │ - Health checks  │  │ - Validation     │
    │ - Backend control│  │ - Detection      │
    │ - Status updates │  │ - Configuration  │
    └──────────────────┘  └──────────────────┘

┌───────────────────────────────────────────────────────────────────────┐
│                       External Processes                               │
└───────────────────────────────────────────────────────────────────────┘
                                      │
                ┌─────────────────────┼─────────────────────┐
                │                     │                     │
                ▼                     ▼                     ▼
    ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
    │  ASP.NET Core    │  │     FFmpeg       │  │  Auto-Updater    │
    │    Backend       │  │                  │  │                  │
    │                  │  │ - Video render   │  │ - Update checks  │
    │ - REST API       │  │ - Encoding       │  │ - Downloads      │
    │ - WebSocket/SSE  │  │ - Transcoding    │  │ - Installation   │
    │ - Port: dynamic  │  │                  │  │                  │
    └──────────────────┘  └──────────────────┘  └──────────────────┘

┌───────────────────────────────────────────────────────────────────────┐
│                          Renderer Process                              │
│                        (React Application)                             │
└───────────────────────────────────────────────────────────────────────┘
                                      │
                                      │ window.electron API
                                      │
                                      ▼
                          ┌────────────────────┐
                          │   React Frontend   │
                          │  (Aura.Web/dist)   │
                          │                    │
                          │  - UI Components   │
                          │  - Video Creation  │
                          │  - Settings        │
                          │  - Project Mgmt    │
                          └────────────────────┘

┌───────────────────────────────────────────────────────────────────────┐
│                        Security Boundaries                             │
└───────────────────────────────────────────────────────────────────────┘

    Renderer Process              │              Main Process
    (Sandboxed)                   │              (Privileged)
                                  │
    - No Node.js access           │         - Full Node.js access
    - No filesystem access        │         - Filesystem access
    - No process spawning         │         - Process spawning
    - No native modules           │         - Native modules
                                  │
    ─────────────────────────────┼─────────────────────────────
                                  │
              ▲                   │                   ▼
              │                   │                   │
         window.electron          │            contextBridge
         (Exposed API)            │            (Whitelist)
                                  │
                              preload.js
                        (Security Bridge)

┌───────────────────────────────────────────────────────────────────────┐
│                     Configuration Validation                           │
└───────────────────────────────────────────────────────────────────────┘

    validate-electron-config.js
              │
              ├─► Check package.json
              ├─► Verify entry point
              ├─► Validate modules (9)
              ├─► Validate IPC handlers (5)
              ├─► Check initialization
              └─► Verify dependencies

    ✅ All checks passed
    ⚠️  1 warning (legacy file)

┌───────────────────────────────────────────────────────────────────────┐
│                         File Structure                                 │
└───────────────────────────────────────────────────────────────────────┘

Aura.Desktop/
├── package.json                     ← "main": "electron/main.js"
│
├── electron/                        ← Modular architecture
│   ├── main.js                     ← Entry point (orchestrator)
│   ├── window-manager.js           ← Window lifecycle
│   ├── backend-service.js          ← Backend management
│   ├── app-config.js               ← Configuration
│   ├── tray-manager.js             ← System tray
│   ├── menu-builder.js             ← App menu
│   ├── protocol-handler.js         ← URL protocol
│   ├── windows-setup-wizard.js     ← First-run setup
│   ├── preload.js                  ← IPC bridge
│   ├── types.d.ts                  ← TypeScript defs
│   └── ipc-handlers/               ← IPC channels
│       ├── config-handler.js
│       ├── system-handler.js
│       ├── video-handler.js
│       ├── backend-handler.js
│       └── ffmpeg-handler.js
│
├── scripts/
│   ├── validate-electron-config.js  ← NEW: Validation
│   ├── validate-build-config.js     ← Build validation
│   └── sign-windows.js              ← Code signing
│
├── ELECTRON_CONFIG_VERIFICATION.md  ← NEW: Documentation
└── electron.js                      ← Legacy (not used)

┌───────────────────────────────────────────────────────────────────────┐
│                         Key Features                                   │
└───────────────────────────────────────────────────────────────────────┘

✅ Modular Architecture      - Separation of concerns
✅ Security Hardened         - Context isolation, sandboxing
✅ Auto-Updates             - Seamless background updates
✅ Backend Management       - Automatic spawn and health checks
✅ Configuration Validation  - Automated integrity checks
✅ Windows Integration      - First-run wizard, file associations
✅ System Tray             - Quick access and notifications
✅ DevTools Support        - Development and debugging
✅ Protocol Handler        - Deep linking (aura:// URLs)
✅ Encrypted Storage       - Secure configuration and API keys

┌───────────────────────────────────────────────────────────────────────┐
│                      Status: ✅ VERIFIED                              │
│                   All Requirements Met                                │
└───────────────────────────────────────────────────────────────────────┘
