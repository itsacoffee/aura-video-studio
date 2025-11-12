# Aura Video Studio Icons

This folder contains the complete icon set for the Aura Video Studio application across all platforms and use cases.

## Icon Files

### Application Icon
- **app_icon.ico** (102 KB)
  - Multi-resolution Windows ICO file
  - Contains embedded sizes: 16x16, 32x32, 48x48, 64x64, 128x128, 256x256
  - Used for: Desktop application window, taskbar, installer, and file associations

### PNG Icons
Individual PNG files for web and mobile use:

| File | Size | Dimensions | Primary Use |
|------|------|------------|-------------|
| icon_16x16.png | 658 bytes | 16×16 | Browser favicon (small) |
| icon_32x32.png | 1.8 KB | 32×32 | Browser favicon (standard) |
| icon_64x64.png | 5.4 KB | 64×64 | Medium resolution displays |
| icon_128x128.png | 18.2 KB | 128×128 | High resolution displays |
| icon_256x256.png | 68.7 KB | 256×256 | Apple touch icon, PWA |
| icon_512x512.png | 273.5 KB | 512×512 | Apple touch icon, splash screens |

## Usage

### Web Frontend (Aura.Web)

Icons are copied to `Aura.Web/public/` during build setup:

```
Icons/icon_16x16.png    → Aura.Web/public/favicon-16x16.png
Icons/icon_32x32.png    → Aura.Web/public/favicon-32x32.png
Icons/app_icon.ico      → Aura.Web/public/favicon.ico
Icons/icon_256x256.png  → Aura.Web/public/logo256.png
Icons/icon_512x512.png  → Aura.Web/public/logo512.png
```

Referenced in `Aura.Web/index.html`:
```html
<link rel="icon" type="image/x-icon" href="/favicon.ico" />
<link rel="icon" type="image/png" sizes="16x16" href="/favicon-16x16.png" />
<link rel="icon" type="image/png" sizes="32x32" href="/favicon-32x32.png" />
<link rel="apple-touch-icon" sizes="256x256" href="/logo256.png" />
<link rel="apple-touch-icon" sizes="512x512" href="/logo512.png" />
```

**How it works:**
- Vite automatically copies files from `public/` folder to the build output (`dist/`)
- Files in `public/` are served from the root path in both dev and production
- No import needed - files are referenced by path in HTML

### Desktop Application (Aura.Desktop)

The multi-resolution ICO file is copied to `Aura.Desktop/assets/icons/icon.ico`.

Referenced in `Aura.Desktop/package.json`:
```json
{
  "build": {
    "win": {
      "icon": "assets/icons/icon.ico"
    },
    "nsis": {
      "installerIcon": "assets/icons/icon.ico",
      "uninstallerIcon": "assets/icons/icon.ico",
      "installerHeaderIcon": "assets/icons/icon.ico"
    },
    "fileAssociations": [
      {
        "ext": "aura",
        "icon": "assets/icons/icon.ico"
      }
    ]
  }
}
```

**How it works:**
- Electron Builder uses the ICO file during Windows application packaging
- The icon appears in: application window, taskbar, Alt+Tab switcher, installer, and file associations
- Multi-resolution ICO ensures sharp icons at all Windows DPI settings

## Icon Design Specifications

### Dimensions
All icons maintain consistent aspect ratio and visual weight across sizes:
- **16x16**: Simplified design, core brand elements only
- **32x32**: Standard detail level, primary use case
- **64x64**: Full detail, smooth rendering
- **128x128+**: High resolution for Retina/HiDPI displays

### Format Guidelines
- **ICO format**: Required for Windows desktop application
  - Must contain multiple embedded sizes (16, 32, 48, 64, 128, 256)
  - Use 32-bit color depth with alpha transparency
- **PNG format**: Preferred for web
  - 32-bit RGBA (with alpha channel)
  - Optimized file sizes without quality loss

### Color Profile
- **Color space**: sRGB
- **Bit depth**: 32-bit (24-bit RGB + 8-bit alpha)
- **Background**: Transparent

### Design Notes
The Aura logo features:
- Gradient color scheme (brand colors)
- Clean, modern aesthetic
- High contrast for visibility at small sizes
- Distinctive shape for brand recognition

## Build Process Integration

### Web Build
1. Developer places PNG files in `Aura.Web/public/`
2. Vite build copies all `public/` contents to `dist/` (root level)
3. HTML references icons by absolute path (e.g., `/favicon.ico`)
4. Production build includes all icon files unchanged

### Desktop Build
1. Developer places `icon.ico` in `Aura.Desktop/assets/icons/`
2. Electron Builder reads path from `package.json`
3. During packaging, Electron Builder:
   - Extracts appropriate icon sizes for different contexts
   - Embeds icons in the EXE file
   - Generates installer with branded icons
   - Configures file associations with the icon

## Updating Icons

To update the icon set:

1. **Replace source files** in the `Icons/` folder
2. **Copy to web frontend**:
   ```bash
   cp Icons/icon_16x16.png Aura.Web/public/favicon-16x16.png
   cp Icons/icon_32x32.png Aura.Web/public/favicon-32x32.png
   cp Icons/app_icon.ico Aura.Web/public/favicon.ico
   cp Icons/icon_256x256.png Aura.Web/public/logo256.png
   cp Icons/icon_512x512.png Aura.Web/public/logo512.png
   ```
3. **Copy to desktop app**:
   ```bash
   cp Icons/app_icon.ico Aura.Desktop/assets/icons/icon.ico
   ```
4. **Test in development**:
   - Web: `cd Aura.Web && npm run dev` (check browser tab icon)
   - Desktop: `cd Aura.Desktop && npm run dev` (check window icon)
5. **Build and verify**:
   - Web: `cd Aura.Web && npm run build` (check `dist/` folder)
   - Desktop: `cd Aura.Desktop && npm run build:dir` (check `dist/` folder)

## Troubleshooting

### Web Icons Not Appearing
- Clear browser cache (Ctrl+Shift+Delete)
- Hard refresh (Ctrl+F5)
- Check DevTools Console for 404 errors
- Verify files exist in `Aura.Web/public/`
- Check build output: files should be in `dist/` folder

### Desktop Icon Not Updating
- Delete `Aura.Desktop/dist` folder
- Rebuild: `npm run build:dir`
- On Windows, icon cache may need refresh:
  ```bash
  ie4uinit.exe -show
  ie4uinit.exe -ClearIconCache
  ```
- Restart Windows Explorer (Task Manager → Windows Explorer → Restart)

### ICO File Issues
- Verify ICO contains multiple sizes: use IcoFX or similar tool
- Check file is valid ICO format (not renamed PNG)
- Ensure 32-bit color depth with alpha channel
- File size should be ~100KB for multi-resolution ICO

## References

- [Vite Static Asset Handling](https://vitejs.dev/guide/assets.html#the-public-directory)
- [Electron Builder Icons Configuration](https://www.electron.build/icons)
- [Favicon Best Practices (2024)](https://evilmartians.com/chronicles/how-to-favicon-in-2021-six-files-that-fit-most-needs)
- [Windows ICO Format Specification](https://en.wikipedia.org/wiki/ICO_(file_format))
