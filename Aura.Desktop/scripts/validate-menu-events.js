/**
 * Validation Script: Menu Event Channel Completeness
 * 
 * This script validates that all menu event channels sent by menu-builder.js
 * are properly defined in MENU_EVENT_CHANNELS and exposed in preload.js
 */

const fs = require('fs');
const path = require('path');
const { MENU_EVENT_CHANNELS } = require('../electron/menu-event-types');

console.log('='.repeat(60));
console.log('Validating Menu Event System Completeness');
console.log('='.repeat(60));

let allValid = true;

// Step 1: Extract all _sendToRenderer calls from menu-builder.js
console.log('\n1. Scanning menu-builder.js for event channels...');
const menuBuilderPath = path.join(__dirname, '../electron/menu-builder.js');
const menuBuilderContent = fs.readFileSync(menuBuilderPath, 'utf8');

// Find all _sendToRenderer('channel') calls
const sendToRendererRegex = /_sendToRenderer\(['"]([^'"]+)['"]/g;
const foundChannels = [];
let match;

while ((match = sendToRendererRegex.exec(menuBuilderContent)) !== null) {
  foundChannels.push(match[1]);
}

console.log(`   Found ${foundChannels.length} _sendToRenderer calls`);
foundChannels.forEach(channel => {
  console.log(`   - ${channel}`);
});

// Step 2: Check if all found channels are in MENU_EVENT_CHANNELS
console.log('\n2. Validating channels against MENU_EVENT_CHANNELS...');
const missingFromDefinition = foundChannels.filter(channel => !MENU_EVENT_CHANNELS.includes(channel));

if (missingFromDefinition.length > 0) {
  console.error('   ✗ VALIDATION FAILED: Channels missing from MENU_EVENT_CHANNELS:');
  missingFromDefinition.forEach(channel => {
    console.error(`     - ${channel}`);
  });
  allValid = false;
} else {
  console.log('   ✓ All channels from menu-builder.js are in MENU_EVENT_CHANNELS');
}

// Step 3: Check if all MENU_EVENT_CHANNELS are actually used
console.log('\n3. Checking for unused channels in MENU_EVENT_CHANNELS...');
const unusedChannels = MENU_EVENT_CHANNELS.filter(channel => !foundChannels.includes(channel));

if (unusedChannels.length > 0) {
  console.warn('   ⚠ Warning: Channels defined but not used in menu-builder.js:');
  unusedChannels.forEach(channel => {
    console.warn(`     - ${channel}`);
  });
  console.warn('   These may be intentional for future features.');
}

// Step 4: Extract menu API methods from preload.js
console.log('\n4. Validating preload.js exposes all channels...');
const preloadPath = path.join(__dirname, '../electron/preload.js');
const preloadContent = fs.readFileSync(preloadPath, 'utf8');

// Find all menu: { ... } section and extract on* methods
const menuAPIRegex = /menu:\s*\{([^}]+)\}/s;
const menuAPIMatch = menuAPIRegex.exec(preloadContent);

if (!menuAPIMatch) {
  console.error('   ✗ VALIDATION FAILED: Could not find menu API in preload.js');
  allValid = false;
} else {
  const menuAPIContent = menuAPIMatch[1];
  
  // Extract method names: on*: (callback) => safeOn('channel', callback)
  const methodRegex = /(on[A-Z][a-zA-Z]+):/g;
  const exposedMethods = [];
  let methodMatch;
  
  while ((methodMatch = methodRegex.exec(menuAPIContent)) !== null) {
    exposedMethods.push(methodMatch[1]);
  }
  
  console.log(`   Found ${exposedMethods.length} exposed methods in preload.js`);
  
  // Convert MENU_EVENT_CHANNELS to expected method names
  const expectedMethods = MENU_EVENT_CHANNELS.map(channel => {
    // Convert 'menu:newProject' to 'onNewProject'
    return 'on' + channel
      .replace('menu:', '')
      .split(/[-:]/)
      .map((part, index) => index === 0 
        ? part.charAt(0).toUpperCase() + part.slice(1)
        : part.charAt(0).toUpperCase() + part.slice(1))
      .join('');
  });
  
  // Check for missing methods
  const missingMethods = expectedMethods.filter(method => !exposedMethods.includes(method));
  
  if (missingMethods.length > 0) {
    console.error('   ✗ VALIDATION FAILED: Methods missing from preload.js menu API:');
    missingMethods.forEach((method, index) => {
      console.error(`     - ${method} (for channel: ${MENU_EVENT_CHANNELS[expectedMethods.indexOf(method)]})`);
    });
    allValid = false;
  } else {
    console.log('   ✓ All channels are exposed in preload.js menu API');
  }
}

// Step 5: Validate TypeScript type file
console.log('\n5. Validating TypeScript type definitions...');
const typesPath = path.join(__dirname, '../../Aura.Web/src/types/electron-menu.ts');

if (!fs.existsSync(typesPath)) {
  console.error('   ✗ VALIDATION FAILED: electron-menu.ts not found');
  allValid = false;
} else {
  const typesContent = fs.readFileSync(typesPath, 'utf8');
  
  // Check if MenuAPI interface exists
  if (!typesContent.includes('export interface MenuAPI')) {
    console.error('   ✗ VALIDATION FAILED: MenuAPI interface not found in electron-menu.ts');
    allValid = false;
  } else {
    console.log('   ✓ TypeScript types file exists with MenuAPI interface');
  }
}

// Step 6: Summary
console.log('\n' + '='.repeat(60));
if (allValid) {
  console.log('VALIDATION PASSED ✓');
  console.log('='.repeat(60));
  console.log('Summary:');
  console.log(`  - Menu channels in menu-builder.js: ${foundChannels.length}`);
  console.log(`  - Channels in MENU_EVENT_CHANNELS: ${MENU_EVENT_CHANNELS.length}`);
  console.log(`  - All channels properly validated and exposed`);
  console.log('='.repeat(60));
  process.exit(0);
} else {
  console.error('VALIDATION FAILED ✗');
  console.error('='.repeat(60));
  console.error('Please fix the issues above and run validation again.');
  console.error('='.repeat(60));
  process.exit(1);
}
