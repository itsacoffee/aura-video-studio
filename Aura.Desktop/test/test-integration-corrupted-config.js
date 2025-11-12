/**
 * Integration test for corrupted config file handling
 * Tests that the application can start in degraded mode when config is corrupted
 */

const os = require('os');
const path = require('path');
const fs = require('fs');

// Mock app object
const mockApp = {
  getVersion: () => '1.0.0-test',
  getPath: (name) => {
    if (name === 'userData') {
      return path.join(os.tmpdir(), 'aura-config-test');
    }
    if (name === 'temp') {
      return os.tmpdir();
    }
    if (name === 'cache') {
      return path.join(os.tmpdir(), 'aura-config-test-cache');
    }
    return os.tmpdir();
  }
};

// Load modules
const { InitializationTracker, InitializationStep } = require('../electron/initialization-tracker');
const EarlyCrashLogger = require('../electron/early-crash-logger');
const SafeInit = require('../electron/safe-initialization');
const StartupLogger = require('../electron/startup-logger');

console.log('='.repeat(60));
console.log('Integration Test: Corrupted Config File Handling');
console.log('='.repeat(60));

let passed = 0;
let failed = 0;

function test(description, fn) {
  return new Promise((resolve) => {
    Promise.resolve(fn())
      .then(() => {
        console.log(`✓ ${description}`);
        passed++;
        resolve();
      })
      .catch((error) => {
        console.error(`✗ ${description}`);
        console.error('  Error:', error.message);
        failed++;
        resolve();
      });
  });
}

function assertTrue(value, message) {
  if (!value) {
    throw new Error(message);
  }
}

function assertEquals(actual, expected, message) {
  if (actual !== expected) {
    throw new Error(`${message}: expected ${expected}, got ${actual}`);
  }
}

async function runTests() {
  // Setup: Create corrupted config file
  console.log('\nSetup: Creating test environment with corrupted config...');
  
  const userDataPath = mockApp.getPath('userData');
  if (!fs.existsSync(userDataPath)) {
    fs.mkdirSync(userDataPath, { recursive: true });
  }
  
  // Create a corrupted electron-store config file
  const configPath = path.join(userDataPath, 'config.json');
  fs.writeFileSync(configPath, '{ corrupted json data [[[', 'utf8');
  console.log('  Created corrupted config file:', configPath);
  
  // Test 1: Initialize crash logger
  console.log('\n1. Testing early crash logger initialization...');
  await test('Should initialize crash logger before config', async () => {
    const crashLogger = new EarlyCrashLogger(mockApp);
    assertTrue(crashLogger.isInitialized, 'Crash logger should initialize');
    assertTrue(fs.existsSync(crashLogger.crashFile), 'Crash log file should exist');
  });
  
  // Test 2: Initialize tracker
  console.log('\n2. Testing initialization tracker...');
  await test('Should initialize tracker', async () => {
    const tracker = new InitializationTracker(mockApp);
    assertTrue(tracker.steps.size > 0, 'Tracker should have steps');
  });
  
  // Test 3: Initialize startup logger
  console.log('\n3. Testing startup logger...');
  await test('Should initialize startup logger', async () => {
    const logger = new StartupLogger(mockApp, { debugMode: true });
    assertTrue(logger.getLogFile() !== null, 'Log file should be created');
  });
  
  // Test 4: Try to initialize config with corruption
  console.log('\n4. Testing app config initialization with corrupted file...');
  await test('Should handle corrupted config gracefully', async () => {
    const tracker = new InitializationTracker(mockApp);
    const crashLogger = new EarlyCrashLogger(mockApp);
    const logger = new StartupLogger(mockApp, { debugMode: false });
    
    const result = SafeInit.initializeAppConfig(
      mockApp, 
      tracker, 
      logger, 
      crashLogger
    );
    
    // Should succeed in degraded mode
    assertTrue(result.success, 'Initialization should succeed');
    assertTrue(result.degradedMode, 'Should be in degraded mode');
    assertTrue(result.component !== null, 'Should have a working config component');
    assertTrue(result.error !== null, 'Should have captured the original error');
    
    // Verify tracker recorded the degraded state
    const status = tracker.getStepStatus(InitializationStep.APP_CONFIG);
    assertEquals(status.status, 'success', 'Step should be marked as success');
    assertTrue(status.metadata.degradedMode, 'Metadata should indicate degraded mode');
  });
  
  // Test 5: Verify degraded config functionality
  console.log('\n5. Testing degraded mode config functionality...');
  await test('Should provide basic config operations in degraded mode', async () => {
    const tracker = new InitializationTracker(mockApp);
    const crashLogger = new EarlyCrashLogger(mockApp);
    const logger = new StartupLogger(mockApp, { debugMode: false });
    
    const result = SafeInit.initializeAppConfig(
      mockApp, 
      tracker, 
      logger, 
      crashLogger
    );
    
    const config = result.component;
    
    // Test basic operations
    assertEquals(config.get('theme', 'light'), 'dark', 'Should return default value');
    
    config.set('testKey', 'testValue');
    assertEquals(config.get('testKey'), 'testValue', 'Should store and retrieve values');
    
    assertTrue(config.isFirstRun(), 'Should report first run in degraded mode');
    
    const paths = config.getPaths();
    assertTrue(paths.userData !== null, 'Should return valid paths');
  });
  
  // Test 6: Verify complete initialization can proceed
  console.log('\n6. Testing that initialization can continue after config failure...');
  await test('Should allow initialization to continue with degraded config', async () => {
    const tracker = new InitializationTracker(mockApp);
    const crashLogger = new EarlyCrashLogger(mockApp);
    const logger = new StartupLogger(mockApp, { debugMode: false });
    
    // Initialize config (will be degraded)
    const configResult = SafeInit.initializeAppConfig(
      mockApp, 
      tracker, 
      logger, 
      crashLogger
    );
    
    assertTrue(configResult.success, 'Config should initialize');
    assertTrue(configResult.degradedMode, 'Config should be degraded');
    
    // Verify tracker state
    assertTrue(tracker.allCriticalStepsSucceeded(), 'Critical steps should succeed even with degraded config');
    
    const summary = tracker.getSummary();
    assertEquals(summary.criticalFailureCount, 0, 'Should have no critical failures');
  });
  
  // Test 7: Verify crash logger captured the error
  console.log('\n7. Testing that crash logger captured config error...');
  await test('Should log config initialization failure', async () => {
    const crashLogger = new EarlyCrashLogger(mockApp);
    const tracker = new InitializationTracker(mockApp);
    const logger = new StartupLogger(mockApp, { debugMode: false });
    
    SafeInit.initializeAppConfig(mockApp, tracker, logger, crashLogger);
    
    // Check crash log contains error
    const crashLog = fs.readFileSync(crashLogger.crashFile, 'utf8');
    assertTrue(
      crashLog.includes('INITIALIZATION_FAILURE') || crashLog.includes('app-config'),
      'Crash log should mention config failure'
    );
  });
  
  // Test 8: Recovery action available
  console.log('\n8. Testing recovery action is provided...');
  await test('Should provide recovery action for config failure', async () => {
    const tracker = new InitializationTracker(mockApp);
    const crashLogger = new EarlyCrashLogger(mockApp);
    const logger = new StartupLogger(mockApp, { debugMode: false });
    
    const result = SafeInit.initializeAppConfig(
      mockApp, 
      tracker, 
      logger, 
      crashLogger
    );
    
    assertTrue(result.recoveryAction !== null, 'Should provide recovery action');
    assertTrue(
      result.recoveryAction.length > 0,
      'Recovery action should not be empty'
    );
  });
  
  // Test 9: Startup logger tracks degraded mode
  console.log('\n9. Testing startup logger tracks degraded mode...');
  await test('Should log degraded mode in startup logger', async () => {
    const crashLogger = new EarlyCrashLogger(mockApp);
    const tracker = new InitializationTracker(mockApp);
    const logger = new StartupLogger(mockApp, { debugMode: false });
    
    SafeInit.initializeAppConfig(mockApp, tracker, logger, crashLogger);
    
    const logFile = logger.getLogFile();
    const logContent = fs.readFileSync(logFile, 'utf8');
    
    // Should contain warning or error about config
    assertTrue(
      logContent.includes('AppConfig') || logContent.includes('Configuration'),
      'Startup log should mention config'
    );
  });
  
  // Test 10: Multiple degraded components
  console.log('\n10. Testing multiple components in degraded mode...');
  await test('Should handle multiple degraded components correctly', async () => {
    const tracker = new InitializationTracker(mockApp);
    const crashLogger = new EarlyCrashLogger(mockApp);
    const logger = new StartupLogger(mockApp, { debugMode: false });
    
    // Initialize config (degraded)
    const configResult = SafeInit.initializeAppConfig(
      mockApp, 
      tracker, 
      logger, 
      crashLogger
    );
    
    assertTrue(configResult.degradedMode, 'Config should be degraded');
    
    // Count degraded features
    const degradedCount = Array.from(tracker.steps.values())
      .filter(s => s.metadata.degradedMode).length;
    
    assertTrue(degradedCount > 0, 'Should have at least one degraded feature');
  });
  
  // Cleanup
  console.log('\n11. Cleaning up test environment...');
  const testDir = path.join(os.tmpdir(), 'aura-config-test');
  if (fs.existsSync(testDir)) {
    fs.rmSync(testDir, { recursive: true, force: true });
    console.log('✓ Cleanup complete');
  }
  
  // Summary
  console.log('\n' + '='.repeat(60));
  console.log('TEST RESULTS');
  console.log('='.repeat(60));
  console.log(`Passed: ${passed}`);
  console.log(`Failed: ${failed}`);
  console.log(`Total: ${passed + failed}`);
  console.log('='.repeat(60));
  
  if (failed > 0) {
    process.exit(1);
  } else {
    console.log('\nALL INTEGRATION TESTS PASSED ✓');
  }
}

// Run tests
runTests().catch(error => {
  console.error('\nTest suite failed:', error);
  process.exit(1);
});
