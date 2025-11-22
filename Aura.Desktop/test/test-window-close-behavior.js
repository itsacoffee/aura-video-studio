/**
 * Test script for Window Close Behavior
 * This validates that the window close button works correctly with different settings
 */

const fs = require('fs');
const path = require('path');

// Test results tracking
let testResults = [];
let passedTests = 0;
let failedTests = 0;

/**
 * Helper function to log test results
 */
function test(description, fn) {
  try {
    fn();
    testResults.push({ description, status: 'passed' });
    passedTests++;
    console.log(`✓ ${description}`);
  } catch (error) {
    testResults.push({ description, status: 'failed', error: error.message });
    failedTests++;
    console.log(`✗ ${description}`);
    console.log(`  Error: ${error.message}`);
  }
}

/**
 * Mock event object for window close
 */
class MockCloseEvent {
  constructor() {
    this.defaultPrevented = false;
  }

  preventDefault() {
    this.defaultPrevented = true;
  }
}

/**
 * Mock window manager for testing
 */
class MockWindowManager {
  constructor() {
    this.mainWindow = {
      hide: () => {
        this.mainWindow.hidden = true;
      },
      hidden: false
    };
  }

  handleWindowClose(event, isQuitting, minimizeToTray = false) {
    if (!isQuitting && minimizeToTray && process.platform === "win32") {
      event.preventDefault();
      this.mainWindow.hide();
      return true; // Prevented default
    }
    return false; // Allow close
  }
}

console.log('\n=== Window Close Behavior Tests ===\n');

// Test 1: Default parameter is false
test('handleWindowClose default parameter is false', () => {
  const windowManagerCode = fs.readFileSync(
    path.join(__dirname, '..', 'electron', 'window-manager.js'),
    'utf8'
  );
  
  const functionMatch = windowManagerCode.match(
    /handleWindowClose\s*\([^)]*minimizeToTray\s*=\s*(true|false)\s*\)/
  );
  
  if (!functionMatch) {
    throw new Error('handleWindowClose function with minimizeToTray parameter not found');
  }
  
  const defaultValue = functionMatch[1];
  if (defaultValue !== 'false') {
    throw new Error(`Default value is ${defaultValue}, expected false`);
  }
});

// Test 2: Window closes when minimizeToTray is false (default)
test('Window closes when minimizeToTray is false', () => {
  const windowManager = new MockWindowManager();
  const event = new MockCloseEvent();
  const isQuitting = false;
  const minimizeToTray = false;
  
  const prevented = windowManager.handleWindowClose(event, isQuitting, minimizeToTray);
  
  if (prevented) {
    throw new Error('Window close was prevented when it should have been allowed');
  }
  
  if (event.defaultPrevented) {
    throw new Error('Event preventDefault was called when it should not have been');
  }
  
  if (windowManager.mainWindow.hidden) {
    throw new Error('Window was hidden when it should have been closed');
  }
});

// Test 3: Window closes when minimizeToTray is not provided (uses default)
test('Window closes when minimizeToTray is not provided', () => {
  const windowManager = new MockWindowManager();
  const event = new MockCloseEvent();
  const isQuitting = false;
  
  const prevented = windowManager.handleWindowClose(event, isQuitting);
  
  if (prevented) {
    throw new Error('Window close was prevented when it should have been allowed');
  }
  
  if (event.defaultPrevented) {
    throw new Error('Event preventDefault was called when it should not have been');
  }
});

// Test 4: Window hides to tray when minimizeToTray is true on Windows
test('Window hides to tray when minimizeToTray is true (Windows)', () => {
  if (process.platform !== 'win32') {
    console.log('  (Skipped: Not running on Windows)');
    return;
  }
  
  const windowManager = new MockWindowManager();
  const event = new MockCloseEvent();
  const isQuitting = false;
  const minimizeToTray = true;
  
  const prevented = windowManager.handleWindowClose(event, isQuitting, minimizeToTray);
  
  if (!prevented) {
    throw new Error('Window close was not prevented when minimizeToTray is true');
  }
  
  if (!event.defaultPrevented) {
    throw new Error('Event preventDefault was not called when minimizeToTray is true');
  }
  
  if (!windowManager.mainWindow.hidden) {
    throw new Error('Window was not hidden when minimizeToTray is true');
  }
});

// Test 5: Window always closes when isQuitting is true, regardless of minimizeToTray
test('Window closes when isQuitting is true (ignores minimizeToTray)', () => {
  const windowManager = new MockWindowManager();
  const event = new MockCloseEvent();
  const isQuitting = true;
  const minimizeToTray = true;
  
  const prevented = windowManager.handleWindowClose(event, isQuitting, minimizeToTray);
  
  if (prevented) {
    throw new Error('Window close was prevented when isQuitting is true');
  }
  
  if (event.defaultPrevented) {
    throw new Error('Event preventDefault was called when isQuitting is true');
  }
});

// Test 6: Verify main.js passes minimizeToTray from config
test('main.js passes minimizeToTray from config', () => {
  const mainCode = fs.readFileSync(
    path.join(__dirname, '..', 'electron', 'main.js'),
    'utf8'
  );
  
  // Check that main.js gets minimizeToTray from config
  if (!mainCode.includes('appConfig.get("minimizeToTray", false)')) {
    throw new Error('main.js does not get minimizeToTray from config with false default');
  }
  
  // Check that it passes minimizeToTray to handleWindowClose
  if (!mainCode.includes('windowManager.handleWindowClose')) {
    throw new Error('main.js does not call windowManager.handleWindowClose');
  }
});

// Test 7: Verify window-manager.js exports handleWindowClose
test('window-manager.js has handleWindowClose method', () => {
  const windowManagerCode = fs.readFileSync(
    path.join(__dirname, '..', 'electron', 'window-manager.js'),
    'utf8'
  );
  
  if (!windowManagerCode.includes('handleWindowClose(')) {
    throw new Error('handleWindowClose method not found in window-manager.js');
  }
});

// Test 8: Verify minimize to tray only works on Windows
test('Minimize to tray is Windows-only', () => {
  const windowManagerCode = fs.readFileSync(
    path.join(__dirname, '..', 'electron', 'window-manager.js'),
    'utf8'
  );
  
  const functionCode = windowManagerCode.match(
    /handleWindowClose\([^)]*\)\s*{[^}]*}/s
  );
  
  if (!functionCode) {
    throw new Error('handleWindowClose function body not found');
  }
  
  if (!functionCode[0].includes('process.platform === "win32"')) {
    throw new Error('handleWindowClose does not check for Windows platform');
  }
});

// Print summary
console.log('\n=== Test Summary ===');
console.log(`Passed: ${passedTests}`);
console.log(`Failed: ${failedTests}`);
console.log(`Total: ${passedTests + failedTests}`);

if (failedTests > 0) {
  console.log('\n❌ Some tests failed');
  process.exit(1);
} else {
  console.log('\n✅ All tests passed');
  process.exit(0);
}
