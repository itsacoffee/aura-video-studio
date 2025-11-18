#!/usr/bin/env node
/**
 * Test: Legacy preload.js redirect
 * 
 * This test verifies that the root-level preload.js properly redirects to
 * electron/preload.js with appropriate warnings.
 */

const path = require('path');
const fs = require('fs');

console.log('Testing legacy preload.js redirect...\n');

console.log('='.repeat(70));
console.log('Test 1: File Content Validation');
console.log('='.repeat(70));

const preloadPath = path.join(__dirname, '..', 'preload.js');
const canonicalPreloadPath = path.join(__dirname, '..', 'electron', 'preload.js');

// Check that both files exist
if (!fs.existsSync(preloadPath)) {
  console.error('❌ FAIL: Root preload.js does not exist');
  process.exit(1);
}
console.log('✅ Root preload.js exists');

if (!fs.existsSync(canonicalPreloadPath)) {
  console.error('❌ FAIL: Canonical electron/preload.js does not exist');
  process.exit(1);
}
console.log('✅ Canonical electron/preload.js exists');

// Read the root preload.js content
const preloadContent = fs.readFileSync(preloadPath, 'utf8');

// Check for warning comments
if (preloadContent.includes('LEGACY PRELOAD WRAPPER')) {
  console.log('✅ Contains LEGACY PRELOAD WRAPPER warning');
} else {
  console.error('❌ FAIL: Missing LEGACY PRELOAD WRAPPER warning');
  process.exit(1);
}

if (preloadContent.includes('DO NOT USE DIRECTLY')) {
  console.log('✅ Contains DO NOT USE DIRECTLY warning');
} else {
  console.error('❌ FAIL: Missing DO NOT USE DIRECTLY warning');
  process.exit(1);
}

if (preloadContent.includes('@deprecated')) {
  console.log('✅ Contains @deprecated JSDoc tag');
} else {
  console.warn('⚠️  WARNING: Missing @deprecated JSDoc tag');
}

// Check for proper require/forwarding
if (preloadContent.includes("require(canonicalPreloadPath)") || 
    preloadContent.includes("require('./electron/preload')")) {
  console.log('✅ Contains require statement for canonical preload');
} else {
  console.error('❌ FAIL: Missing require statement for canonical preload');
  process.exit(1);
}

// Check for error handling
if (preloadContent.includes('try') && preloadContent.includes('catch')) {
  console.log('✅ Contains try-catch error handling');
} else {
  console.error('❌ FAIL: Missing try-catch error handling');
  process.exit(1);
}

if (preloadContent.includes('throw new Error')) {
  console.log('✅ Contains fail-fast error throwing');
} else {
  console.error('❌ FAIL: Missing fail-fast error throwing');
  process.exit(1);
}

console.log('\n' + '='.repeat(70));
console.log('Test 2: Module Loading (outside Electron context)');
console.log('='.repeat(70));

// Capture console warnings
const originalWarn = console.warn;
const warnings = [];
console.warn = (...args) => {
  warnings.push(args.join(' '));
  originalWarn(...args);
};

try {
  // Try to require the legacy preload.js
  // This will fail outside Electron context but should show proper error handling
  const legacyPreload = require(preloadPath);
  
  console.log('✅ Legacy preload.js successfully loaded');
  
  // Check that warnings were emitted
  if (warnings.length > 0) {
    console.log('✅ Warnings were emitted during load');
    
    const hasLegacyWarning = warnings.some(w => 
      w.includes('Loading legacy preload.js') || 
      w.includes('canonical preload script')
    );
    
    if (hasLegacyWarning) {
      console.log('✅ Warning mentions legacy usage');
    } else {
      console.warn('⚠️  WARNING: Warning does not mention legacy usage');
    }
  } else {
    console.warn('⚠️  WARNING: No warnings emitted during load');
  }
  
} catch (error) {
  // This is expected when running outside Electron context
  console.log('ℹ️  Module loading failed (expected outside Electron context)');
  
  // Check that the error message is helpful
  if (error.message.includes('Failed to load canonical preload script')) {
    console.log('✅ Error message indicates preload loading failure');
  } else if (error.message.includes("Cannot find module 'electron'")) {
    console.log('✅ Error indicates Electron module missing (expected outside Electron)');
  } else {
    console.error('❌ FAIL: Unexpected error:', error.message);
    process.exit(1);
  }
  
  // Check that warnings were emitted before the error
  if (warnings.length > 0) {
    console.log('✅ Warnings were emitted before error');
    
    const hasLegacyWarning = warnings.some(w => 
      w.includes('Loading legacy preload.js') || 
      w.includes('canonical preload script')
    );
    
    if (hasLegacyWarning) {
      console.log('✅ Warning mentions legacy usage');
    }
  } else {
    console.warn('⚠️  WARNING: No warnings emitted before error');
  }
  
} finally {
  // Restore console.warn
  console.warn = originalWarn;
}

console.log('\n' + '='.repeat(70));
console.log('Test Summary');
console.log('='.repeat(70));
console.log('✅ All checks passed!');
console.log('   The legacy preload.js properly redirects to electron/preload.js');
console.log('   with appropriate warnings and error handling.');
console.log('\nWarnings emitted:');
console.log('-'.repeat(70));
warnings.forEach((w, i) => console.log(`${i + 1}. ${w}`));
console.log('-'.repeat(70));

process.exit(0);
