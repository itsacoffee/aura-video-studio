# How to Launch Aura Video Studio

## 🚀 Quick Launch Guide

There are several ways to run Aura Video Studio depending on your needs:

---

## Option 1: Development Mode (Recommended for Testing)

This launches the Electron desktop app with the built frontend:

```bash
cd Aura.Desktop
npm start
```

**Requirements:**
- Frontend must be built first (see below)
- Backend is automatically started by Electron
- Opens in a native desktop window

---

## Option 2: Full Desktop App (Production Build)

If you've built the installer:

### Windows
- Double-click the installer: `Aura-Video-Studio-Setup-1.0.0.exe`
- Or run the portable version: `Aura-Video-Studio-1.0.0-portable.exe`

### macOS
- Open the DMG: `Aura-Video-Studio-1.0.0-universal.dmg`
- Drag to Applications folder
- Launch from Applications

### Linux
- Run the AppImage: `./Aura-Video-Studio-1.0.0.AppImage`
- Or install the .deb: `sudo dpkg -i Aura-Video-Studio_1.0.0_amd64.deb`

---

## Option 3: Web Development Mode

Run the frontend and backend separately for development:

### Terminal 1 - Backend API
```bash
cd Aura.Api
dotnet run
```

Backend will start on: http://localhost:5000

### Terminal 2 - Frontend Dev Server
```bash
cd Aura.Web
npm run dev
```

Frontend dev server will start on: http://localhost:5173

**Note:** This mode requires manual configuration and is best for frontend development.

---

## Option 4: Docker (Full Stack)

Run the complete stack in Docker containers:

```bash
# From workspace root
docker-compose up
```

Access at: http://localhost:5000

---

## 🔧 Prerequisites

### For Development Mode (Option 1)
1. **Build the frontend first:**
   ```bash
   cd Aura.Web
   npm install
   npm run build
   ```

2. **Install Electron dependencies:**
   ```bash
   cd Aura.Desktop
   npm install
   ```

3. **Launch:**
   ```bash
   npm start
   ```

### For Production Build (Option 2)
Run the build script first:

**Windows:**
```powershell
cd Aura.Desktop
.\build-desktop.ps1
```

**Linux/macOS:**
```bash
cd Aura.Desktop
./build-desktop.sh
```

Installers will be in `Aura.Desktop/dist/`

---

## ⚡ Quick Setup (First Time)

```bash
# 1. Install frontend dependencies
cd Aura.Web
npm install

# 2. Build frontend
npm run build

# 3. Install Electron dependencies
cd ../Aura.Desktop
npm install

# 4. Launch app
npm start
```

---

## 🐛 Troubleshooting

### "Cannot find module" errors
```bash
cd Aura.Desktop
npm install
```

### "Frontend not found" error
```bash
cd Aura.Web
npm run build
```

### Backend fails to start
- Check if port 5000 is available
- Check logs in Electron DevTools (View → Toggle Developer Tools)
- Verify .NET 8.0 runtime is installed

### White screen / blank app
- Verify frontend was built: `ls ../Aura.Web/dist/index.html`
- Check Electron DevTools console for errors
- Try clearing cache: `rm -rf Aura.Desktop/dist/`

---

## 📝 Notes

- **First Launch:** The app will run a first-time setup wizard to configure AI providers
- **Data Location:** User data is stored in the OS-specific app data folder:
  - Windows: `%APPDATA%\aura-video-studio\`
  - macOS: `~/Library/Application Support/aura-video-studio/`
  - Linux: `~/.config/aura-video-studio/`
- **Logs:** Check logs for debugging in the data location above

---

## 🎬 Ready to Create!

Once launched:
1. Complete the first-run setup wizard
2. Configure your AI provider API keys
3. Start creating amazing videos!

For more details, see:
- [Desktop App Guide](DESKTOP_APP_GUIDE.md)
- [Build Guide](BUILD_GUIDE.md)
- [Development Guide](DEVELOPMENT.md)
