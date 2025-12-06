/**
 * Test for UX fixes to splash screen and menu
 * 
 * Tests verify:
 * 1. Spinner is removed from splash screen
 * 2. Zoom controls are removed from View menu
 * 3. Important elements remain intact
 */

const assert = require('assert');
const fs = require('fs');
const path = require('path');

console.log('='.repeat(60));
console.log('Testing Splash Screen and Menu UX Fixes');
console.log('='.repeat(60));

// Test 1: Verify spinner is removed from splash.html
console.log('\n1. Testing splash screen has no spinner...');
const splashPath = path.join(__dirname, '../electron/splash.html');
const splashContent = fs.readFileSync(splashPath, 'utf8');

assert.ok(!splashContent.includes('.spinner'), 'Splash should not have .spinner CSS class');
assert.ok(!splashContent.includes('@keyframes spin'), 'Splash should not have spin animation');
assert.ok(!splashContent.includes('<div class="spinner">'), 'Splash should not have spinner HTML element');
console.log('✓ Spinner removed from splash screen');

// Test 2: Verify important splash elements remain
console.log('\n2. Testing important splash elements remain...');
assert.ok(splashContent.includes('particles'), 'Splash should have particles canvas');
assert.ok(splashContent.includes('progress-bar'), 'Splash should have progress bar');
assert.ok(splashContent.includes('app-icon'), 'Splash should have app icon');
assert.ok(splashContent.includes('@keyframes shimmer'), 'Splash should have shimmer animation');
assert.ok(splashContent.includes('AI Video Generation Suite'), 'Splash should have tagline');
assert.ok(splashContent.includes('class="logo"'), 'Splash should have logo');
console.log('✓ All important splash elements present');

// Test 3: Verify zoom controls removed from menu-builder.js
console.log('\n3. Testing zoom controls removed from menu...');
const menuPath = path.join(__dirname, '../electron/menu-builder.js');
const menuContent = fs.readFileSync(menuPath, 'utf8');

assert.ok(!menuContent.includes('Zoom In'), 'Menu should not have "Zoom In" option');
assert.ok(!menuContent.includes('Zoom Out'), 'Menu should not have "Zoom Out" option');
assert.ok(!menuContent.includes('Actual Size'), 'Menu should not have "Actual Size" option');
assert.ok(!menuContent.includes('setZoomLevel'), 'Menu should not have setZoomLevel calls');
assert.ok(!menuContent.includes('getZoomLevel'), 'Menu should not have getZoomLevel calls');
console.log('✓ Zoom controls removed from menu');

// Test 4: Verify important menu items remain
console.log('\n4. Testing important menu items remain...');
assert.ok(menuContent.includes('Toggle Full Screen'), 'Menu should have "Toggle Full Screen"');
assert.ok(menuContent.includes('Toggle Developer Tools'), 'Menu should have "Toggle Developer Tools"');
assert.ok(menuContent.includes('Reload'), 'Menu should have "Reload"');
assert.ok(menuContent.includes('Force Reload'), 'Menu should have "Force Reload"');
console.log('✓ All important menu items present');

// Test 5: Verify View menu structure
console.log('\n5. Testing View menu structure...');
// Just verify the View menu still has expected items
assert.ok(menuContent.includes('Toggle Developer Tools'), 'View menu should have developer tools');
assert.ok(menuContent.includes('Toggle Full Screen'), 'View menu should have full screen toggle');
console.log('✓ View menu structure correct');

console.log('\n' + '='.repeat(60));
console.log('ALL SPLASH AND MENU UX FIX TESTS PASSED ✓');
console.log('='.repeat(60));
console.log('\nSummary:');
console.log('- Spinner removed from splash screen');
console.log('- Zoom controls removed from menu');
console.log('- All important elements remain intact');
console.log('='.repeat(60));
