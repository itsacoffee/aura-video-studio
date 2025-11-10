# Icons and Graphics Guide for Aura Video Studio

This directory contains all icons and graphics needed for the Aura Video Studio desktop application and installers.

## Required Files

### Application Icons

#### **icon.ico** (Windows)
- Format: ICO file containing multiple resolutions
- Required sizes: 16x16, 32x32, 48x48, 64x64, 128x128, 256x256
- Color depth: 32-bit (with alpha transparency)
- Usage: Main application icon, file associations, taskbar
- Tool recommendations: 
  - [IcoFX](https://icofx.ro/) (Windows)
  - [GIMP](https://www.gimp.org/) with ICO plugin (Cross-platform)
  - [ImageMagick](https://imagemagick.org/) (Command-line)

**How to create:**
```bash
# Using ImageMagick
magick convert logo-256.png -define icon:auto-resize=256,128,64,48,32,16 icon.ico
```

#### **icon.icns** (macOS)
- Format: ICNS file containing multiple resolutions
- Required sizes: 16x16, 32x32, 64x64, 128x128, 256x256, 512x512, 1024x1024
- Each size needs @1x and @2x versions (retina display support)
- Color depth: 32-bit with alpha
- Usage: Main application icon, dock, finder
- Tool recommendations:
  - [Image2Icon](http://www.img2icnsapp.com/) (macOS)
  - [iconutil](https://developer.apple.com/library/archive/documentation/GraphicsAnimation/Conceptual/HighResolutionOSX/Optimizing/Optimizing.html) (macOS built-in)

**How to create:**
```bash
# Using iconutil (macOS)
mkdir icon.iconset
# Create all required sizes, then:
iconutil -c icns icon.iconset
```

#### **icon.png** (Linux)
- Format: PNG with transparency
- Size: 512x512 pixels (will be resized for different contexts)
- Color depth: 32-bit RGBA
- Usage: Application icon, desktop shortcuts, app launchers
- Note: Linux desktops will scale this automatically

### Installer Graphics

#### **installer-header.bmp** (Windows NSIS)
- Format: BMP (24-bit, no alpha)
- Size: 150 width x 57 height pixels
- Purpose: Top banner in NSIS installer dialog
- Background: Usually white or light gray
- Content: Small logo or app name
- **Important**: Must be exactly 150x57 pixels

**Design tips:**
- Keep design simple and readable at small size
- Use high contrast for visibility
- Place logo on left, text on right (if any)
- Test with Windows high-DPI displays

#### **installer-sidebar.bmp** (Windows NSIS)
- Format: BMP (24-bit, no alpha)
- Size: 164 width x 314 height pixels
- Purpose: Vertical banner on left side of installer
- Background: Gradient or solid color matching brand
- Content: Logo, app name, version number
- **Important**: Must be exactly 164x314 pixels

**Design tips:**
- Vertical orientation, logo at top
- Keep important elements in top 200 pixels
- Bottom area can be decorative gradient
- Use brand colors for recognition

#### **tray.png** (System Tray)
- Format: PNG with transparency
- Size: 16x16 pixels (small), 32x32 pixels (high-DPI)
- Purpose: System tray/notification area icon
- Requirements:
  - Must be visible on both light and dark taskbars
  - Simple, iconic design (recognizable at tiny size)
  - Consider using monochrome or simple two-color design
- Filename variations:
  - `tray.png` - Default tray icon
  - `tray@2x.png` - High-DPI version (optional)

**Design tips:**
- Test on both light and dark backgrounds
- Very simple design - will be tiny
- Consider just using a letter or simple symbol
- Windows will resize automatically but starts from 16x16

### DMG Background (macOS)

#### **dmg-background.png**
- Format: PNG
- Size: 600 width x 400 height pixels (or 1200x800 for @2x)
- Purpose: Background image for macOS DMG installer window
- Content: Instructions graphic showing drag to Applications folder
- Already configured in package.json

## Design Guidelines

### Brand Colors
Update these with your actual brand colors:
- Primary: #6366F1 (Indigo)
- Secondary: #8B5CF6 (Purple)
- Accent: #EC4899 (Pink)
- Dark: #1F2937 (Gray-900)
- Light: #F9FAFB (Gray-50)

### Logo Requirements
- **Clean and Simple**: Icons must work at 16x16 pixels
- **Distinctive**: Recognizable shape even at small sizes
- **Scalable**: Vector source recommended (SVG, AI, or Figma)
- **Brand Consistent**: Match your website/marketing materials

### Icon Design Best Practices

1. **Start with Vector**
   - Design in vector format (Illustrator, Figma, Inkscape)
   - Export to required raster formats
   - Keep source file for future updates

2. **Test at All Sizes**
   - View icon at 16x16, 32x32, 48x48, etc.
   - Adjust details that become unclear at small sizes
   - May need simplified versions for smallest sizes

3. **Use Transparency Wisely**
   - PNG and ICO support alpha channel
   - BMP for installers does NOT support transparency
   - Test on various backgrounds

4. **Consider Context**
   - Desktop icon: Can be detailed and colorful
   - Tray icon: Must be simple and high-contrast
   - Installer graphics: Professional and branded

## Tools and Resources

### Icon Creation Tools

**Professional (Paid):**
- Adobe Illustrator + Photoshop
- Affinity Designer + Photo
- Sketch (macOS)
- Figma (Web-based)

**Free and Open Source:**
- [GIMP](https://www.gimp.org/) - Raster graphics editor
- [Inkscape](https://inkscape.org/) - Vector graphics editor
- [Krita](https://krita.org/) - Digital painting and icons
- [ImageMagick](https://imagemagick.org/) - Command-line image conversion

### Online Icon Generators

- [IconKitchen](https://icon.kitchen/) - Multi-platform app icons
- [MakeAppIcon](https://makeappicon.com/) - Generate all sizes from one image
- [AppIcon](https://appicon.co/) - macOS and iOS icons

### Icon Resources (Free)

- [Flaticon](https://www.flaticon.com/) - Large icon library
- [Icons8](https://icons8.com/) - Icons in various styles
- [Noun Project](https://thenounproject.com/) - Simple, iconic designs
- [Material Icons](https://fonts.google.com/icons) - Google's icon set

**Important**: Check licenses before using. For commercial apps, you may need to purchase licenses or provide attribution.

## Quick Start: Creating Icons from Logo

If you have a logo file (SVG or high-res PNG), here's how to generate all required icons:

### Using ImageMagick (Command-line)

```bash
# Assume you have logo.png (at least 1024x1024 pixels)

# Windows ICO
magick convert logo.png -resize 256x256 -define icon:auto-resize=256,128,64,48,32,16 icon.ico

# macOS ICNS (requires iconutil on macOS)
mkdir icon.iconset
magick convert logo.png -resize 16x16 icon.iconset/icon_16x16.png
magick convert logo.png -resize 32x32 icon.iconset/icon_16x16@2x.png
magick convert logo.png -resize 32x32 icon.iconset/icon_32x32.png
magick convert logo.png -resize 64x64 icon.iconset/icon_32x32@2x.png
magick convert logo.png -resize 128x128 icon.iconset/icon_128x128.png
magick convert logo.png -resize 256x256 icon.iconset/icon_128x128@2x.png
magick convert logo.png -resize 256x256 icon.iconset/icon_256x256.png
magick convert logo.png -resize 512x512 icon.iconset/icon_256x256@2x.png
magick convert logo.png -resize 512x512 icon.iconset/icon_512x512.png
magick convert logo.png -resize 1024x1024 icon.iconset/icon_512x512@2x.png
iconutil -c icns icon.iconset

# Linux PNG
magick convert logo.png -resize 512x512 icon.png

# Tray icon (simplified version recommended)
magick convert logo.png -resize 16x16 tray.png
magick convert logo.png -resize 32x32 tray@2x.png

# Installer graphics (create these in design software, not just resize)
```

### Using Online Tool

1. Go to [MakeAppIcon](https://makeappicon.com/)
2. Upload your logo (1024x1024 PNG with transparency)
3. Select platforms: Windows, macOS, Linux
4. Download generated icons
5. Copy files to this directory

## Testing Your Icons

### Windows
1. Build installer: `npm run build:win`
2. Install on test system
3. Check:
   - Desktop shortcut icon
   - Start Menu icon
   - Taskbar icon (when app running)
   - Tray icon (notification area)
   - System tray icon (when minimized)
   - File association icon (.aura files)
   - Installer icon and graphics

### macOS
1. Build DMG: `npm run build:mac`
2. Open DMG and drag to Applications
3. Check:
   - Finder icon
   - Dock icon
   - Launchpad icon
   - DMG background image
   - Window title icon

### Linux
1. Build AppImage: `npm run build:linux`
2. Install on test system
3. Check:
   - Application menu icon
   - Desktop shortcut (if created)
   - Window title icon

## Current Status

⚠️ **ICONS NEEDED**: This directory currently only contains this guide. You must create the actual icon files before building installers.

### Required Files Checklist
- [ ] `icon.ico` (Windows, 256x256 multi-resolution)
- [ ] `icon.icns` (macOS, 1024x1024 multi-resolution)
- [ ] `icon.png` (Linux, 512x512)
- [ ] `tray.png` (System tray, 16x16)
- [ ] `installer-header.bmp` (NSIS header, 150x57)
- [ ] `installer-sidebar.bmp` (NSIS sidebar, 164x314)
- [ ] `dmg-background.png` (macOS DMG, 600x400)

### Temporary Solution

Until you create proper icons, you can use a placeholder:

1. Find any high-resolution PNG logo (at least 512x512)
2. Use ImageMagick or an online tool to convert to required formats
3. Replace placeholders with professional icons before public release

### Getting Professional Icons

If you need professional icon design:
1. **Freelancers**: Fiverr, Upwork, 99designs ($50-$500)
2. **Design Studios**: Full branding package ($500-$5000)
3. **DIY**: Use tools listed above (free-$300)

## Questions?

For icon-related questions or issues:
1. Check electron-builder documentation: https://www.electron.build/icons
2. Check this guide's troubleshooting section
3. Search GitHub issues for similar problems
4. Ask in project discussions

---

**Last Updated**: 2025-11-10
**Version**: 1.0.0
