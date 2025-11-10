# Aura Video Studio Icons

This directory contains the application icons for all platforms.

## Required Icon Files

### Windows
- `icon.ico` - Windows application icon (256x256, includes 16x16, 32x32, 48x48, 64x64, 128x128, 256x256)
- `installer-header.bmp` - NSIS installer header (150x57 pixels)

### macOS
- `icon.icns` - macOS application icon (includes 16x16, 32x32, 128x128, 256x256, 512x512, 1024x1024)

### Linux
- `icon.png` - Linux application icon (512x512)
- `16x16.png` - Small icon
- `32x32.png` - Medium icon
- `48x48.png` - Large icon
- `128x128.png` - Extra large icon
- `256x256.png` - Super large icon
- `512x512.png` - Ultra large icon

### System Tray
- `tray.png` - System tray icon (16x16 or 22x22, monochrome recommended)
- `tray@2x.png` - High-DPI system tray icon (32x32 or 44x44)

### Installer Background
- `dmg-background.png` - macOS DMG background image (600x400)

## Creating Icons

### From SVG
If you have an SVG logo, you can use these tools to generate platform icons:

**Windows (.ico):**
```bash
# Using ImageMagick
convert -background none logo.svg -define icon:auto-resize=256,128,64,48,32,16 icon.ico
```

**macOS (.icns):**
```bash
# Create iconset directory
mkdir icon.iconset
sips -z 16 16     logo.png --out icon.iconset/icon_16x16.png
sips -z 32 32     logo.png --out icon.iconset/icon_16x16@2x.png
sips -z 32 32     logo.png --out icon.iconset/icon_32x32.png
sips -z 64 64     logo.png --out icon.iconset/icon_32x32@2x.png
sips -z 128 128   logo.png --out icon.iconset/icon_128x128.png
sips -z 256 256   logo.png --out icon.iconset/icon_128x128@2x.png
sips -z 256 256   logo.png --out icon.iconset/icon_256x256.png
sips -z 512 512   logo.png --out icon.iconset/icon_256x256@2x.png
sips -z 512 512   logo.png --out icon.iconset/icon_512x512.png
sips -z 1024 1024 logo.png --out icon.iconset/icon_512x512@2x.png
iconutil -c icns icon.iconset
```

**Linux (.png):**
```bash
# Use ImageMagick or similar
convert -background none -resize 512x512 logo.svg icon.png
```

## Placeholder Icons

Until you create custom icons, electron-builder will use default Electron icons.
For best results, create your own branded icons following the specifications above.

## Brand Guidelines

- Use the Aura purple gradient (#667eea to #764ba2)
- Maintain consistency across all platforms
- Ensure icons are legible at small sizes (16x16)
- Use transparent backgrounds where supported
- Test icons in both light and dark system themes
