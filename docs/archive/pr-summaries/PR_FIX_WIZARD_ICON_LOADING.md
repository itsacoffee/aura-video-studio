# PR: Fix Setup Wizard Icon Loading and Positioning Issues

**Created:** 2025-11-22
**Author:** @Coffee285
**Status:** Ready for Implementation

## Problem Statement

1. **Icon not loading in .exe (packaged app)**: The setup wizard displays a broken image icon in the progress indicator
2. **Icon positioning**: Even when the icon loads, it's positioned too high and needs to be moved down
3. **Similar issues may exist**: Other components using the Logo component may have the same problem

## Root Cause Analysis

After analyzing the codebase:

1. **FirstRunWizard.tsx** (line 1627) shows "Welcome to Aura Video Studio - Let's get you set up!" with the icon appearing in the progress area
2. **Logo.tsx** component uses relative paths (`/logo256.png`, `/logo512.png`) which work in dev but may fail in packaged Electron apps using `file://` protocol
3. **No explicit logo/icon display** in the wizard header area based on the screenshot
4. The icon appears to be part of the step progress indicator, not a dedicated logo

## Solution

### Part 1: Add Explicit Logo Display to Wizard Header

**File: `Aura.Web/src/pages/Onboarding/FirstRunWizard.tsx`**

Add import at the top:
```tsx
import { Logo } from '../../components/Logo';
```

Update the header section (around line 1623-1644) to include the Logo component:

```tsx
<div
  className={styles.header}
  style={{ textAlign: 'center', paddingTop: tokens.spacingVerticalL }}
>
  {/* Add Logo component */}
  <div style={{ 
    display: 'flex', 
    justifyContent: 'center', 
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM 
  }}>
    <Logo size={64} />
  </div>
  
  <Title2>Welcome to Aura Video Studio - Let&apos;s get you set up!</Title2>
  <Text
    style={{
      display: 'block',
      marginTop: tokens.spacingVerticalXS,
      marginBottom: tokens.spacingVerticalM,
    }}
  >
    Step {state.step + 1} of {totalSteps} - Required Setup
  </Text>
  <WizardProgress
    currentStep={state.step}
    totalSteps={totalSteps}
    stepLabels={stepLabels}
    onStepClick={handleStepClick}
    onSaveAndExit={handleExitWizard}
  />
</div>
```

### Part 2: Fix Logo Component for Electron Packaging

**File: `Aura.Web/src/components/Logo.tsx`**

Replace the entire file with this enhanced version:

```tsx
import { makeStyles } from '@fluentui/react-components';
import { memo, useState, useEffect } from 'react';

const useStyles = makeStyles({
  logo: {
    display: 'inline-block',
  },
  image: {
    display: 'block',
    width: '100%',
    height: '100%',
    objectFit: 'contain',
  },
  fallback: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    width: '100%',
    height: '100%',
    background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
    borderRadius: '12px',
    color: 'white',
    fontWeight: 'bold',
    fontSize: '24px',
  },
});

export interface LogoProps {
  /**
   * Size of the logo in pixels
   */
  size?: number;
  /**
   * Additional CSS class name
   */
  className?: string;
  /**
   * Alt text for the logo image
   */
  alt?: string;
}

/**
 * Logo component that displays the Aura Video Studio icon.
 * Handles both web and Electron (file://) protocols with fallback.
 */
export const Logo = memo<LogoProps>(({ size = 64, className, alt = 'Aura Video Studio' }) => {
  const styles = useStyles();
  const [imageError, setImageError] = useState(false);
  const [imagePath, setImagePath] = useState<string>('');

  useEffect(() => {
    // Determine the correct image path based on environment
    const getIconPath = (requestedSize: number): string => {
      // Base paths to try
      const sizes = [
        { max: 16, paths: ['/favicon-16x16.png', './favicon-16x16.png', 'favicon-16x16.png'] },
        { max: 32, paths: ['/favicon-32x32.png', './favicon-32x32.png', 'favicon-32x32.png'] },
        { max: 128, paths: ['/logo256.png', './logo256.png', 'logo256.png'] },
        { max: Infinity, paths: ['/logo512.png', './logo512.png', 'logo512.png'] },
      ];

      const sizeConfig = sizes.find(s => requestedSize <= s.max);
      if (!sizeConfig) return '/logo512.png';

      // In Electron with file:// protocol, try relative paths first
      const isElectron = window.navigator.userAgent.toLowerCase().includes('electron');
      const isFileProtocol = window.location.protocol === 'file:';

      if (isElectron || isFileProtocol) {
        // Try relative path without leading slash first for file:// protocol
        return sizeConfig.paths[1] || sizeConfig.paths[0];
      }

      // For web/http, use absolute path
      return sizeConfig.paths[0];
    };

    setImagePath(getIconPath(size));
    setImageError(false);
  }, [size]);

  const handleImageError = () => {
    console.warn(`[Logo] Failed to load image from ${imagePath}, showing fallback`);
    setImageError(true);
  };

  if (imageError) {
    // Fallback to gradient with "A" text
    return (
      <span className={className} style={{ width: size, height: size }}>
        <div className={styles.fallback} style={{ fontSize: `${size * 0.5}px` }}>
          A
        </div>
      </span>
    );
  }

  return (
    <span className={className} style={{ width: size, height: size }}>
      <img
        src={imagePath}
        alt={alt}
        className={styles.image}
        width={size}
        height={size}
        draggable={false}
        onError={handleImageError}
        loading="eager"
      />
    </span>
  );
});

Logo.displayName = 'Logo';
```

### Part 3: Ensure Assets Are Included in Electron Build

**File: `Aura.Desktop/package.json`**

Verify the `files` array includes the web assets (should already exist around line 32-45):

```json
{
  "build": {
    "files": [
      "electron/**/*",
      "resources/**/*",
      "!resources/**/*.pdb",
      "!resources/**/*.xml",
      "../Aura.Web/dist/**/*"
    ],
    "extraResources": [
      {
        "from": "../Aura.Web/dist",
        "to": "frontend",
        "filter": ["**/*"]
      }
    ]
  }
}
```

### Part 4: Update Electron Window Manager to Serve Assets Correctly

**File: `Aura.Desktop/electron/window-manager.js`**

Ensure the main window's `loadURL` properly handles asset paths. Around line 200-250, verify or add:

```javascript
if (this.isDev) {
  await this.mainWindow.loadURL('http://127.0.0.1:5173');
} else {
  // Production: Load from bundled frontend
  const frontendPath = path.join(process.resourcesPath, 'frontend', 'index.html');
  await this.mainWindow.loadURL(`file://${frontendPath}`);
  
  // Set base path for assets in file:// protocol
  await this.mainWindow.webContents.executeJavaScript(`
    if (!window.__ASSET_BASE_PATH__) {
      window.__ASSET_BASE_PATH__ = '${path.join(process.resourcesPath, 'frontend').replace(/\\/g, '/')}';
    }
  `);
}
```

## Testing Checklist

- [ ] Logo displays correctly in development mode (npm run dev)
- [ ] Logo displays correctly in packaged .exe application
- [ ] Logo fallback works when image files are missing
- [ ] Logo appears in wizard header with proper spacing
- [ ] Logo size is appropriate (64px) and positioned correctly
- [ ] No console errors about missing images
- [ ] Step progress indicator remains functional
- [ ] Logo displays on all wizard steps (0-5)

## Files Changed

1. `Aura.Web/src/pages/Onboarding/FirstRunWizard.tsx` - Add Logo to header
2. `Aura.Web/src/components/Logo.tsx` - Enhanced with Electron support and fallback
3. `Aura.Desktop/electron/window-manager.js` - Verify asset path handling (if needed)
4. `Aura.Desktop/package.json` - Verify build configuration (if needed)

## Additional Improvements

This PR also includes improvements to make the Logo component more robust:
- Automatic protocol detection (file:// vs http://)
- Graceful fallback to gradient "A" icon if image fails
- Proper error handling and logging
- Eager loading for better UX
- Support for both absolute and relative paths

## Impact

- **Fixes**: Icon loading in packaged .exe
- **Improves**: Icon positioning in setup wizard
- **Prevents**: Similar issues in future by improving Logo component
- **No Breaking Changes**: All changes are additive or improvements

## Related Issues

- Icon not loading in .exe build
- Icon positioning too high in wizard
- Need better asset path handling for Electron
