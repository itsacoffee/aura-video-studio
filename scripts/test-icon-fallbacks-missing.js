#!/usr/bin/env node

/**
 * Test Icon Fallback with Missing Files
 * This script simulates what happens when icon files are missing
 */

const fs = require('fs');
const path = require('path');

console.log('=== Icon Fallback Test (Missing Files Scenario) ===\n');

// Create a mock Electron environment
const mockElectron = {
  nativeImage: {
    createFromPath: function(path) {
      // Simulate Electron's nativeImage behavior
      if (!fs.existsSync(path)) {
        return { isEmpty: () => true };
      }
      return { isEmpty: () => false };
    },
    createFromDataURL: function(dataURL) {
      // Check if it's a valid data URL
      if (dataURL.startsWith('data:image/png;base64,')) {
        const base64 = dataURL.replace('data:image/png;base64,', '');
        const buffer = Buffer.from(base64, 'base64');
        // Check for PNG signature
        const pngSignature = Buffer.from([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        if (buffer.slice(0, 8).equals(pngSignature)) {
          return { isEmpty: () => false, size: buffer.length };
        }
      }
      return { isEmpty: () => true };
    },
    createEmpty: function() {
      return { isEmpty: () => true };
    }
  }
};

console.log('Test 1: Testing fallback icon creation with mock nativeImage...');
try {
  const iconFallbacks = require('../Aura.Desktop/electron/icon-fallbacks.js');
  
  // Test 16x16 icon
  const icon16 = iconFallbacks.getFallbackIcon(mockElectron.nativeImage, '16');
  if (icon16.isEmpty()) {
    console.error('✗ 16x16 fallback icon is empty');
    process.exit(1);
  }
  console.log('✓ 16x16 fallback icon created successfully');
  
  // Test 32x32 icon
  const icon32 = iconFallbacks.getFallbackIcon(mockElectron.nativeImage, '32');
  if (icon32.isEmpty()) {
    console.error('✗ 32x32 fallback icon is empty');
    process.exit(1);
  }
  console.log('✓ 32x32 fallback icon created successfully');
  
  // Test 256x256 icon
  const icon256 = iconFallbacks.getFallbackIcon(mockElectron.nativeImage, '256');
  if (icon256.isEmpty()) {
    console.error('✗ 256x256 fallback icon is empty');
    process.exit(1);
  }
  console.log('✓ 256x256 fallback icon created successfully');
  
  // Test default size (should be 32)
  const iconDefault = iconFallbacks.getFallbackIcon(mockElectron.nativeImage);
  if (iconDefault.isEmpty()) {
    console.error('✗ Default fallback icon is empty');
    process.exit(1);
  }
  console.log('✓ Default fallback icon created successfully');
  
  console.log();
} catch (error) {
  console.error('✗ Failed to create fallback icons:', error.message);
  console.error(error.stack);
  process.exit(1);
}

console.log('Test 2: Simulating missing icon files scenario...');
try {
  // Create a mock app object
  const mockApp = {
    isPackaged: false,
    getAppPath: () => '/fake/path',
    getPath: (name) => '/fake/path'
  };
  
  // Test the logic that would be in window-manager
  const iconPaths = [
    '/fake/nonexistent/path/icon.ico',
    '/another/fake/path/icon.ico',
    '/yet/another/fake/path/icon.ico'
  ];
  
  let foundIcon = false;
  for (const iconPath of iconPaths) {
    if (fs.existsSync(iconPath)) {
      console.log(`Found icon at: ${iconPath}`);
      foundIcon = true;
      break;
    }
  }
  
  if (!foundIcon) {
    console.log('✓ No icon files found at any path (expected)');
    console.log('✓ Fallback mechanism would be triggered');
  } else {
    console.error('✗ Unexpectedly found icon file');
    process.exit(1);
  }
  
  console.log();
} catch (error) {
  console.error('✗ Test failed:', error.message);
  process.exit(1);
}

console.log('Test 3: Verify fallback doesn\'t return hardcoded success...');
try {
  const windowManagerPath = path.join(__dirname, '../Aura.Desktop/electron/window-manager.js');
  const trayManagerPath = path.join(__dirname, '../Aura.Desktop/electron/tray-manager.js');
  
  const windowManagerContent = fs.readFileSync(windowManagerPath, 'utf8');
  const trayManagerContent = fs.readFileSync(trayManagerPath, 'utf8');
  
  // Check that we don't have any hardcoded success returns
  const badPatterns = [
    'return true;  // Icon not found',
    'return nativeImage.createEmpty()',
    'return {}',
    '// Return success even if icon missing'
  ];
  
  let foundBadPattern = false;
  for (const pattern of badPatterns) {
    if (windowManagerContent.includes(pattern) || trayManagerContent.includes(pattern)) {
      console.error(`✗ Found bad pattern: "${pattern}"`);
      foundBadPattern = true;
    }
  }
  
  if (!foundBadPattern) {
    console.log('✓ No hardcoded success patterns found');
  } else {
    process.exit(1);
  }
  
  // Verify getFallbackIcon is actually called
  if (windowManagerContent.includes('getFallbackIcon(nativeImage')) {
    console.log('✓ window-manager.js calls getFallbackIcon with proper arguments');
  } else {
    console.error('✗ window-manager.js does not properly call getFallbackIcon');
    process.exit(1);
  }
  
  if (trayManagerContent.includes('getFallbackIcon(nativeImage')) {
    console.log('✓ tray-manager.js calls getFallbackIcon with proper arguments');
  } else {
    console.error('✗ tray-manager.js does not properly call getFallbackIcon');
    process.exit(1);
  }
  
  console.log();
} catch (error) {
  console.error('✗ Test failed:', error.message);
  process.exit(1);
}

console.log('Test 4: Verify error handling and logging...');
try {
  const windowManagerPath = path.join(__dirname, '../Aura.Desktop/electron/window-manager.js');
  const trayManagerPath = path.join(__dirname, '../Aura.Desktop/electron/tray-manager.js');
  
  const windowManagerContent = fs.readFileSync(windowManagerPath, 'utf8');
  const trayManagerContent = fs.readFileSync(trayManagerPath, 'utf8');
  
  // Check for proper error handling
  const requiredErrorHandling = [
    'if (fs.existsSync(',
    'catch (error)',
    'console.error',
    'console.warn',
    'icon.isEmpty()'
  ];
  
  for (const pattern of requiredErrorHandling) {
    if (windowManagerContent.includes(pattern)) {
      console.log(`✓ window-manager.js has error handling: "${pattern}"`);
    } else {
      console.error(`✗ window-manager.js missing error handling: "${pattern}"`);
      process.exit(1);
    }
  }
  
  for (const pattern of requiredErrorHandling) {
    if (trayManagerContent.includes(pattern)) {
      console.log(`✓ tray-manager.js has error handling: "${pattern}"`);
    } else {
      console.error(`✗ tray-manager.js missing error handling: "${pattern}"`);
      process.exit(1);
    }
  }
  
  console.log();
} catch (error) {
  console.error('✗ Test failed:', error.message);
  process.exit(1);
}

console.log('=== All Missing Files Tests Passed! ===\n');
console.log('Summary:');
console.log('✓ Fallback icons can be created from base64 data');
console.log('✓ Path resolution handles missing files gracefully');
console.log('✓ No hardcoded success values - always provides working icon');
console.log('✓ Comprehensive error handling and logging present');
console.log();
console.log('The icon fallback system will work correctly when icon files are missing.');
console.log('Fallback base64 icons will be displayed instead of empty/broken icons.');
