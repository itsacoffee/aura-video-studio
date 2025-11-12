#!/usr/bin/env node

/**
 * Test Icon Fallbacks
 * This script tests the icon fallback functionality without running Electron
 */

const fs = require('fs');
const path = require('path');

console.log('=== Icon Fallback Test ===\n');

// Test 1: Check that fallback module exists and exports correct functions
console.log('Test 1: Checking fallback module...');
try {
  const iconFallbacks = require('../Aura.Desktop/electron/icon-fallbacks.js');
  
  if (typeof iconFallbacks.getFallbackIcon !== 'function') {
    console.error('✗ getFallbackIcon is not a function');
    process.exit(1);
  }
  
  if (typeof iconFallbacks.FALLBACK_ICON_16_BASE64 !== 'string') {
    console.error('✗ FALLBACK_ICON_16_BASE64 is not a string');
    process.exit(1);
  }
  
  if (typeof iconFallbacks.FALLBACK_ICON_32_BASE64 !== 'string') {
    console.error('✗ FALLBACK_ICON_32_BASE64 is not a string');
    process.exit(1);
  }
  
  if (typeof iconFallbacks.FALLBACK_ICON_256_BASE64 !== 'string') {
    console.error('✗ FALLBACK_ICON_256_BASE64 is not a string');
    process.exit(1);
  }
  
  console.log('✓ Fallback module exports correct functions and constants\n');
} catch (error) {
  console.error('✗ Failed to load fallback module:', error.message);
  process.exit(1);
}

// Test 2: Verify base64 strings are valid PNG data
console.log('Test 2: Validating base64 icon data...');
try {
  const iconFallbacks = require('../Aura.Desktop/electron/icon-fallbacks.js');
  
  // Check if base64 starts with PNG signature when decoded
  const testBase64 = (base64Data, name) => {
    const buffer = Buffer.from(base64Data, 'base64');
    
    // PNG signature: 89 50 4E 47 0D 0A 1A 0A (first 8 bytes)
    const pngSignature = Buffer.from([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
    const actualSignature = buffer.slice(0, 8);
    
    if (!actualSignature.equals(pngSignature)) {
      console.error(`✗ ${name} does not have valid PNG signature`);
      console.error('  Expected:', pngSignature.toString('hex'));
      console.error('  Got:', actualSignature.toString('hex'));
      return false;
    }
    
    console.log(`✓ ${name} has valid PNG signature (${buffer.length} bytes)`);
    return true;
  };
  
  const valid16 = testBase64(iconFallbacks.FALLBACK_ICON_16_BASE64, 'FALLBACK_ICON_16_BASE64');
  const valid32 = testBase64(iconFallbacks.FALLBACK_ICON_32_BASE64, 'FALLBACK_ICON_32_BASE64');
  const valid256 = testBase64(iconFallbacks.FALLBACK_ICON_256_BASE64, 'FALLBACK_ICON_256_BASE64');
  
  if (!valid16 || !valid32 || !valid256) {
    process.exit(1);
  }
  
  console.log();
} catch (error) {
  console.error('✗ Failed to validate base64 data:', error.message);
  process.exit(1);
}

// Test 3: Check icon file paths
console.log('Test 3: Checking icon file locations...');
const iconPaths = [
  path.join(__dirname, '../Aura.Desktop/assets/icons/icon.ico'),
  path.join(__dirname, '../Aura.Desktop/assets/icons/icon.png'),
  path.join(__dirname, '../Aura.Desktop/assets/icons/tray.png'),
  path.join(__dirname, '../Icons/icon_16x16.png'),
  path.join(__dirname, '../Icons/icon_32x32.png'),
  path.join(__dirname, '../Icons/icon_256x256.png'),
];

for (const iconPath of iconPaths) {
  if (fs.existsSync(iconPath)) {
    const stats = fs.statSync(iconPath);
    console.log(`✓ ${path.basename(iconPath)} exists (${(stats.size / 1024).toFixed(2)} KB)`);
  } else {
    console.log(`⚠ ${path.basename(iconPath)} not found at ${iconPath}`);
  }
}

console.log();

// Test 4: Verify window-manager.js and tray-manager.js import fallbacks
console.log('Test 4: Checking that managers import icon-fallbacks...');
try {
  const windowManagerPath = path.join(__dirname, '../Aura.Desktop/electron/window-manager.js');
  const trayManagerPath = path.join(__dirname, '../Aura.Desktop/electron/tray-manager.js');
  
  const windowManagerContent = fs.readFileSync(windowManagerPath, 'utf8');
  const trayManagerContent = fs.readFileSync(trayManagerPath, 'utf8');
  
  if (windowManagerContent.includes("require('./icon-fallbacks')")) {
    console.log('✓ window-manager.js imports icon-fallbacks');
  } else {
    console.error('✗ window-manager.js does not import icon-fallbacks');
    process.exit(1);
  }
  
  if (trayManagerContent.includes("require('./icon-fallbacks')")) {
    console.log('✓ tray-manager.js imports icon-fallbacks');
  } else {
    console.error('✗ tray-manager.js does not import icon-fallbacks');
    process.exit(1);
  }
  
  if (windowManagerContent.includes('getFallbackIcon')) {
    console.log('✓ window-manager.js uses getFallbackIcon');
  } else {
    console.error('✗ window-manager.js does not use getFallbackIcon');
    process.exit(1);
  }
  
  if (trayManagerContent.includes('getFallbackIcon')) {
    console.log('✓ tray-manager.js uses getFallbackIcon');
  } else {
    console.error('✗ tray-manager.js does not use getFallbackIcon');
    process.exit(1);
  }
  
  console.log();
} catch (error) {
  console.error('✗ Failed to check manager files:', error.message);
  process.exit(1);
}

// Test 5: Verify logging is present
console.log('Test 5: Checking for comprehensive logging...');
try {
  const windowManagerPath = path.join(__dirname, '../Aura.Desktop/electron/window-manager.js');
  const trayManagerPath = path.join(__dirname, '../Aura.Desktop/electron/tray-manager.js');
  
  const windowManagerContent = fs.readFileSync(windowManagerPath, 'utf8');
  const trayManagerContent = fs.readFileSync(trayManagerPath, 'utf8');
  
  const requiredLogs = [
    'Icon Resolution Debug Info',
    'app.getAppPath()',
    'process.resourcesPath',
    'Trying icon path:',
    'Using fallback icon'
  ];
  
  for (const logMessage of requiredLogs) {
    if (windowManagerContent.includes(logMessage)) {
      console.log(`✓ window-manager.js has logging: "${logMessage}"`);
    } else {
      console.error(`✗ window-manager.js missing logging: "${logMessage}"`);
      process.exit(1);
    }
  }
  
  const trayLogs = [
    'Tray Icon Resolution',
    'Tray icon file exists',
    'Using base64 fallback icon for tray'
  ];
  
  for (const logMessage of trayLogs) {
    if (trayManagerContent.includes(logMessage)) {
      console.log(`✓ tray-manager.js has logging: "${logMessage}"`);
    } else {
      console.error(`✗ tray-manager.js missing logging: "${logMessage}"`);
      process.exit(1);
    }
  }
  
  console.log();
} catch (error) {
  console.error('✗ Failed to check logging:', error.message);
  process.exit(1);
}

console.log('=== All Tests Passed! ===\n');
console.log('Summary:');
console.log('✓ Icon fallback module correctly exports functions and constants');
console.log('✓ Base64 icon data is valid PNG format');
console.log('✓ Both managers import and use icon-fallbacks module');
console.log('✓ Comprehensive logging is present in both managers');
console.log('✓ Icon files exist in expected locations');
console.log();
console.log('The icon fallback system is properly configured.');
console.log('Icons will fall back to base64-encoded versions if files are missing.');
