/**
 * Test script for EarlyCrashLogger
 * Tests early crash logging with deliberate error scenarios
 */

const os = require('os');
const path = require('path');
const fs = require('fs');

// Mock app object
const mockApp = {
  getVersion: () => '1.0.0-test',
  getPath: (name) => {
    if (name === 'userData') {
      return path.join(os.tmpdir(), 'aura-crash-test-logs');
    }
    return os.tmpdir();
  }
};

// Load module
const EarlyCrashLogger = require('../electron/early-crash-logger');

console.log('='.repeat(60));
console.log('Testing EarlyCrashLogger');
console.log('='.repeat(60));

let passed = 0;
let failed = 0;

function test(description, fn) {
  try {
    fn();
    console.log(`✓ ${description}`);
    passed++;
  } catch (error) {
    console.error(`✗ ${description}`);
    console.error('  Error:', error.message);
    failed++;
  }
}

function assertTrue(value, message) {
  if (!value) {
    throw new Error(message);
  }
}

function assertFalse(value, message) {
  if (value) {
    throw new Error(message);
  }
}

function assertFileContains(filePath, searchString, message) {
  const content = fs.readFileSync(filePath, 'utf8');
  if (!content.includes(searchString)) {
    throw new Error(`${message}: "${searchString}" not found in file`);
  }
}

// Test 1: Initialization
console.log('\n1. Testing initialization...');
test('Should initialize crash logger and create log file', () => {
  const logger = new EarlyCrashLogger(mockApp);
  
  assertTrue(logger.isInitialized, 'Logger should be initialized');
  assertTrue(logger.crashFile !== null, 'Crash file path should be set');
  assertTrue(fs.existsSync(logger.crashFile), 'Crash log file should exist');
  
  // Verify header was written
  assertFileContains(logger.crashFile, 'AURA VIDEO STUDIO - CRASH LOG', 'Header should be present');
  assertFileContains(logger.crashFile, process.platform, 'Platform should be logged');
});

// Test 2: Log crash
console.log('\n2. Testing crash logging...');
test('Should log crashes with error details', () => {
  const logger = new EarlyCrashLogger(mockApp);
  const error = new Error('Test crash error');
  error.code = 'TEST_ERROR';
  
  logger.logCrash('TEST_CRASH', 'This is a test crash', error, { testData: 'test' });
  
  assertTrue(fs.existsSync(logger.crashFile), 'Crash log should exist');
  assertFileContains(logger.crashFile, 'TEST_CRASH', 'Crash type should be logged');
  assertFileContains(logger.crashFile, 'This is a test crash', 'Crash message should be logged');
  assertFileContains(logger.crashFile, 'Test crash error', 'Error message should be logged');
  assertFileContains(logger.crashFile, 'TEST_ERROR', 'Error code should be logged');
});

// Test 3: Log uncaught exception
console.log('\n3. Testing uncaught exception logging...');
test('Should log uncaught exceptions', () => {
  const logger = new EarlyCrashLogger(mockApp);
  const error = new Error('Uncaught exception test');
  error.stack = 'Error: Uncaught exception test\n    at test.js:123:45';
  
  logger.logUncaughtException(error);
  
  assertFileContains(logger.crashFile, 'UNCAUGHT_EXCEPTION', 'Exception type should be logged');
  assertFileContains(logger.crashFile, 'Uncaught exception test', 'Exception message should be logged');
  assertFileContains(logger.crashFile, 'STACK TRACE', 'Stack trace should be logged');
});

// Test 4: Log unhandled rejection
console.log('\n4. Testing unhandled rejection logging...');
test('Should log unhandled rejections', () => {
  const logger = new EarlyCrashLogger(mockApp);
  const error = new Error('Unhandled promise rejection');
  
  logger.logUnhandledRejection(error);
  
  assertFileContains(logger.crashFile, 'UNHANDLED_REJECTION', 'Rejection type should be logged');
  assertFileContains(logger.crashFile, 'Unhandled promise rejection', 'Rejection message should be logged');
});

// Test 5: Log initialization failure
console.log('\n5. Testing initialization failure logging...');
test('Should log initialization failures with step information', () => {
  const logger = new EarlyCrashLogger(mockApp);
  const error = new Error('Backend failed to start');
  
  logger.logInitializationFailure('backend-service', error, { 
    port: 5005,
    reason: 'Process exited with code 1'
  });
  
  assertFileContains(logger.crashFile, 'INITIALIZATION_FAILURE', 'Failure type should be logged');
  assertFileContains(logger.crashFile, 'backend-service', 'Step name should be logged');
  assertFileContains(logger.crashFile, 'Backend failed to start', 'Error should be logged');
  assertFileContains(logger.crashFile, 'initializationStep', 'Metadata should be logged');
});

// Test 6: Log startup complete
console.log('\n6. Testing startup completion logging...');
test('Should log successful startup completion', () => {
  const logger = new EarlyCrashLogger(mockApp);
  
  logger.logStartupComplete();
  
  assertFileContains(logger.crashFile, 'STARTUP COMPLETED SUCCESSFULLY', 'Success message should be logged');
  assertFileContains(logger.crashFile, 'No critical errors', 'No errors message should be present');
});

// Test 7: Get paths
console.log('\n7. Testing path getters...');
test('Should return correct paths', () => {
  const logger = new EarlyCrashLogger(mockApp);
  
  const crashLogPath = logger.getCrashLogPath();
  const logsDir = logger.getLogsDirectory();
  
  assertTrue(crashLogPath !== null, 'Crash log path should not be null');
  assertTrue(logsDir !== null, 'Logs directory should not be null');
  assertTrue(crashLogPath.includes(logsDir), 'Crash log should be in logs directory');
  assertTrue(fs.existsSync(logsDir), 'Logs directory should exist');
});

// Test 8: Multiple crashes
console.log('\n8. Testing multiple crash logging...');
test('Should handle multiple crashes in sequence', () => {
  const logger = new EarlyCrashLogger(mockApp);
  
  for (let i = 0; i < 5; i++) {
    const error = new Error(`Crash ${i}`);
    logger.logCrash('MULTIPLE_CRASH', `Crash number ${i}`, error);
  }
  
  const content = fs.readFileSync(logger.crashFile, 'utf8');
  const crashMatches = content.match(/MULTIPLE_CRASH/g);
  assertTrue(crashMatches && crashMatches.length >= 5, 'All crashes should be logged');
});

// Test 9: Crash without error object
console.log('\n9. Testing crash logging without error object...');
test('Should handle crashes without error object', () => {
  const logger = new EarlyCrashLogger(mockApp);
  
  logger.logCrash('NO_ERROR_OBJECT', 'Crash without error object', null, { info: 'test' });
  
  assertFileContains(logger.crashFile, 'NO_ERROR_OBJECT', 'Crash type should be logged');
  assertFileContains(logger.crashFile, 'Crash without error object', 'Message should be logged');
  assertFileContains(logger.crashFile, 'info', 'Metadata should be logged');
});

// Test 10: Crash with non-Error object
console.log('\n10. Testing crash logging with non-Error object...');
test('Should handle crashes with string errors', () => {
  const logger = new EarlyCrashLogger(mockApp);
  
  logger.logUnhandledRejection('String error message');
  
  assertFileContains(logger.crashFile, 'UNHANDLED_REJECTION', 'Rejection should be logged');
  assertFileContains(logger.crashFile, 'String error message', 'String error should be logged');
});

// Test 11: Global handlers installation
console.log('\n11. Testing global handlers installation...');
test('Should install global error handlers', () => {
  const logger = new EarlyCrashLogger(mockApp);
  
  // This should not throw
  logger.installGlobalHandlers();
  
  assertTrue(logger.isInitialized, 'Logger should remain initialized');
  assertFileContains(logger.crashFile, 'global handlers installed', 'Installation should be logged');
});

// Test 12: Deliberate error scenario - Crash sequence
console.log('\n12. Testing deliberate error scenario - Crash sequence...');
test('Should handle realistic crash sequence', () => {
  const logger = new EarlyCrashLogger(mockApp);
  
  // Simulate initialization failures
  logger.logInitializationFailure('app-config', new Error('Config file corrupted'));
  logger.logInitializationFailure('backend-service', new Error('Port already in use'));
  
  // Simulate uncaught exception
  const criticalError = new Error('Critical system error');
  criticalError.stack = 'Error: Critical system error\n    at main.js:456:78';
  logger.logUncaughtException(criticalError);
  
  // Verify all errors are logged
  const content = fs.readFileSync(logger.crashFile, 'utf8');
  assertTrue(content.includes('INITIALIZATION_FAILURE'), 'Init failures should be logged');
  assertTrue(content.includes('Config file corrupted'), 'Config error should be logged');
  assertTrue(content.includes('Port already in use'), 'Backend error should be logged');
  assertTrue(content.includes('UNCAUGHT_EXCEPTION'), 'Exception should be logged');
  assertTrue(content.includes('Critical system error'), 'Critical error should be logged');
});

// Cleanup
console.log('\n13. Cleaning up test files...');
const logsDir = path.join(os.tmpdir(), 'aura-crash-test-logs');
if (fs.existsSync(logsDir)) {
  fs.rmSync(logsDir, { recursive: true, force: true });
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
  console.log('\nALL TESTS PASSED ✓');
}
