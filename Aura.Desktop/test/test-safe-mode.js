/**
 * Test Safe Mode Functionality
 * Tests safe mode detection, activation, and recovery
 */

const { strict: assert } = require('assert');
const fs = require('fs');
const path = require('path');

// Mock app for testing
const mockApp = {
  getPath: (name) => {
    if (name === 'userData') {
      return path.join(__dirname, '../temp-test-data-safemode');
    }
    return '/tmp';
  },
  getVersion: () => '1.0.0-test'
};

const AppConfig = require('../electron/app-config');

// Use unique config name for testing
const originalStoreName = 'aura-config';
const testStoreName = `aura-config-test-${Date.now()}`;

// Patch the Store to use test name
const Store = require('electron-store');
const originalStore = Store.prototype.constructor;

console.log('============================================================');
console.log('Testing Safe Mode Functionality');
console.log('============================================================\n');

let appConfig;
const testDir = mockApp.getPath('userData');

// Clean up test directory
function cleanup() {
  try {
    if (fs.existsSync(testDir)) {
      fs.rmSync(testDir, { recursive: true, force: true });
    }
  } catch (error) {
    // Ignore cleanup errors
  }
}

// Setup test directory
function setup() {
  try {
    cleanup();
    // Wait a bit for cleanup
    const start = Date.now();
    while (Date.now() - start < 100) {
      // Wait
    }
    fs.mkdirSync(testDir, { recursive: true });
  } catch (error) {
    console.error('Setup error:', error);
    throw error;
  }
}

async function runTests() {
  let passed = 0;
  let failed = 0;

  // Test 1: Initial crash count should be 0
  console.log('1. Testing initial crash count...');
  setup();
  appConfig = new AppConfig(mockApp);
  const initialCount = appConfig.getCrashCount();
  assert.strictEqual(initialCount, 0, 'Initial crash count should be 0');
  console.log('✓ Initial crash count is 0\n');
  passed++;

  // Test 2: Increment crash count
  console.log('2. Testing crash count increment...');
  const count1 = appConfig.incrementCrashCount();
  assert.strictEqual(count1, 1, 'Crash count should be 1 after first increment');
  const count2 = appConfig.incrementCrashCount();
  assert.strictEqual(count2, 2, 'Crash count should be 2 after second increment');
  console.log('✓ Crash count increments correctly\n');
  passed++;

  // Test 3: Crash count persists across restarts
  console.log('3. Testing crash count persistence...');
  const crashCount = appConfig.getCrashCount();
  const lastCrashTime = appConfig.getLastCrashTime();
  assert.strictEqual(crashCount, 2, 'Crash count should persist');
  assert.ok(lastCrashTime > 0, 'Last crash time should be set');
  
  // Create new instance (simulates restart)
  const appConfig2 = new AppConfig(mockApp);
  const persistedCount = appConfig2.getCrashCount();
  assert.strictEqual(persistedCount, 2, 'Crash count should persist across restart');
  console.log('✓ Crash count persists across restarts\n');
  passed++;

  // Test 4: Should not enter safe mode with 2 crashes
  console.log('4. Testing safe mode threshold...');
  const shouldEnterSafeMode2 = appConfig2.shouldEnterSafeMode(3);
  assert.strictEqual(shouldEnterSafeMode2, false, 'Should not enter safe mode with 2 crashes');
  console.log('✓ Safe mode not triggered below threshold\n');
  passed++;

  // Test 5: Should enter safe mode with 3 crashes
  console.log('5. Testing safe mode activation...');
  appConfig2.incrementCrashCount();
  const shouldEnterSafeMode3 = appConfig2.shouldEnterSafeMode(3);
  assert.strictEqual(shouldEnterSafeMode3, true, 'Should enter safe mode with 3 crashes');
  console.log('✓ Safe mode triggered at threshold\n');
  passed++;

  // Test 6: Safe mode flag persistence
  console.log('6. Testing safe mode flag...');
  appConfig2.enableSafeMode();
  assert.strictEqual(appConfig2.isSafeMode(), true, 'Safe mode should be enabled');
  
  const appConfig3 = new AppConfig(mockApp);
  assert.strictEqual(appConfig3.isSafeMode(), true, 'Safe mode should persist');
  console.log('✓ Safe mode flag persists\n');
  passed++;

  // Test 7: Reset crash count
  console.log('7. Testing crash count reset...');
  appConfig3.resetCrashCount();
  assert.strictEqual(appConfig3.getCrashCount(), 0, 'Crash count should be reset to 0');
  assert.strictEqual(appConfig3.getLastCrashTime(), null, 'Last crash time should be null');
  console.log('✓ Crash count resets correctly\n');
  passed++;

  // Test 8: Disable safe mode
  console.log('8. Testing safe mode disable...');
  appConfig3.disableSafeMode();
  assert.strictEqual(appConfig3.isSafeMode(), false, 'Safe mode should be disabled');
  console.log('✓ Safe mode disables correctly\n');
  passed++;

  // Test 9: Old crashes should not trigger safe mode
  console.log('9. Testing old crash expiration (24 hours)...');
  setup();
  const appConfig4 = new AppConfig(mockApp);
  
  // Explicitly disable safe mode first
  appConfig4.disableSafeMode();
  appConfig4.incrementCrashCount();
  appConfig4.incrementCrashCount();
  appConfig4.incrementCrashCount();
  
  // Manually set last crash time to 25 hours ago
  const twentyFiveHoursAgo = Date.now() - (25 * 60 * 60 * 1000);
  appConfig4.set('lastCrashTime', twentyFiveHoursAgo);
  
  // Should not be in safe mode yet
  assert.strictEqual(appConfig4.isSafeMode(), false, 'Should not be in safe mode initially');
  
  const shouldEnterAfterExpiry = appConfig4.shouldEnterSafeMode(3);
  assert.strictEqual(shouldEnterAfterExpiry, false, 'Old crashes should not trigger safe mode');
  assert.strictEqual(appConfig4.getCrashCount(), 0, 'Crash count should be reset for old crashes');
  console.log('✓ Old crashes expire after 24 hours\n');
  passed++;

  // Test 10: Config file deletion
  console.log('10. Testing config file deletion...');
  setup();
  const appConfig5 = new AppConfig(mockApp);
  appConfig5.set('testKey', 'testValue');
  
  const configPath = appConfig5.getConfigPath();
  assert.ok(fs.existsSync(configPath), 'Config file should exist');
  
  const deleted = appConfig5.deleteConfigFile();
  assert.strictEqual(deleted, true, 'deleteConfigFile should return true');
  assert.ok(!fs.existsSync(configPath), 'Config file should be deleted');
  console.log('✓ Config file deletes successfully\n');
  passed++;

  // Cleanup
  cleanup();

  // Summary
  console.log('============================================================');
  console.log('TEST RESULTS');
  console.log('============================================================');
  console.log(`Passed: ${passed}`);
  console.log(`Failed: ${failed}`);
  console.log(`Total: ${passed + failed}`);
  console.log('============================================================\n');

  return failed === 0;
}

// Run tests
runTests().then((success) => {
  if (!success) {
    process.exit(1);
  }
}).catch((error) => {
  console.error('Test failed with error:', error);
  cleanup();
  process.exit(1);
});
