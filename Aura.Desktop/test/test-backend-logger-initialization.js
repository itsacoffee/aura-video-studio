/**
 * Test: Backend Service Logger Initialization
 * Validates that the logger parameter is properly initialized in BackendService
 * and that all logger methods (info, error, debug) work correctly.
 */

const assert = require('assert');

console.log('=== Backend Service Logger Initialization Tests ===\n');

let passed = 0;
let failed = 0;

function test(description, fn) {
  try {
    fn();
    console.log('✓', description);
    passed++;
  } catch (error) {
    console.log('✗', description);
    console.log('  Error:', error.message);
    failed++;
  }
}

// Mock objects for BackendService instantiation
const mockApp = { getPath: () => '/tmp' };
const mockProcessManager = { register: () => {} };
const mockNetworkContract = { 
  baseUrl: 'http://localhost:5000',
  port: 5000,
  shouldSelfHost: true
};

// Test 1: Constructor accepts logger parameter
test('BackendService constructor accepts logger parameter', () => {
  const BackendService = require('../electron/backend-service.js');
  const constructorStr = BackendService.toString();
  assert.ok(constructorStr.includes('logger'), 'Constructor should have logger parameter');
});

// Test 2: Logger is initialized with fallback to console
test('Logger is initialized with fallback to console', () => {
  const BackendService = require('../electron/backend-service.js');
  const constructorStr = BackendService.toString();
  assert.ok(
    constructorStr.includes('logger || console') || constructorStr.includes('logger ?? console'),
    'Logger should fallback to console when not provided'
  );
});

// Test 3: BackendService can be instantiated without logger (using fallback)
test('BackendService can be instantiated without logger parameter', () => {
  const BackendService = require('../electron/backend-service.js');
  const service = new BackendService(mockApp, true, mockProcessManager, mockNetworkContract);
  assert.ok(service.logger, 'Logger should be defined');
  assert.strictEqual(typeof service.logger.info, 'function', 'Logger should have info method');
  assert.strictEqual(typeof service.logger.error, 'function', 'Logger should have error method');
  assert.strictEqual(typeof service.logger.debug, 'function', 'Logger should have debug method');
});

// Test 4: BackendService can be instantiated with custom logger
test('BackendService can be instantiated with custom logger', () => {
  const BackendService = require('../electron/backend-service.js');
  const mockLogger = {
    info: () => {},
    error: () => {},
    debug: () => {}
  };
  const service = new BackendService(mockApp, true, mockProcessManager, mockNetworkContract, mockLogger);
  assert.strictEqual(service.logger, mockLogger, 'Logger should be the provided custom logger');
});

// Test 5: waitForReady method uses logger
test('waitForReady method uses this.logger', () => {
  const BackendService = require('../electron/backend-service.js');
  const methodStr = BackendService.prototype.waitForReady.toString();
  assert.ok(methodStr.includes('this.logger'), 'waitForReady should use this.logger');
});

// Test 6: waitForReady uses logger.info
test('waitForReady uses logger.info for health check messages', () => {
  const BackendService = require('../electron/backend-service.js');
  const methodStr = BackendService.prototype.waitForReady.toString();
  assert.ok(methodStr.includes('this.logger.info'), 'waitForReady should use this.logger.info');
});

// Test 7: waitForReady uses logger.error
test('waitForReady uses logger.error for error messages', () => {
  const BackendService = require('../electron/backend-service.js');
  const methodStr = BackendService.prototype.waitForReady.toString();
  assert.ok(methodStr.includes('this.logger.error'), 'waitForReady should use this.logger.error');
});

// Test 8: waitForReady uses logger.debug
test('waitForReady uses logger.debug for debug messages', () => {
  const BackendService = require('../electron/backend-service.js');
  const methodStr = BackendService.prototype.waitForReady.toString();
  assert.ok(methodStr.includes('this.logger.debug'), 'waitForReady should use this.logger.debug');
});

// Test 9: Logger calls use optional chaining (safe access)
test('Logger calls use optional chaining for safety', () => {
  const BackendService = require('../electron/backend-service.js');
  const methodStr = BackendService.prototype.waitForReady.toString();
  // Check for optional chaining pattern like .info?.( or .error?.( or .debug?.(
  const hasOptionalChaining = 
    methodStr.includes('.info?.(') || 
    methodStr.includes('.error?.(') || 
    methodStr.includes('.debug?.(');
  assert.ok(hasOptionalChaining, 'Logger calls should use optional chaining for safety');
});

// Test 10: Verify logger actually works during instantiation
test('Logger works correctly during BackendService instantiation', () => {
  const BackendService = require('../electron/backend-service.js');
  
  let infoCallCount = 0;
  let errorCallCount = 0;
  let debugCallCount = 0;
  
  const mockLogger = {
    info: (...args) => { infoCallCount++; },
    error: (...args) => { errorCallCount++; },
    debug: (...args) => { debugCallCount++; }
  };
  
  // Create service - should not throw errors
  const service = new BackendService(mockApp, true, mockProcessManager, mockNetworkContract, mockLogger);
  
  // Verify service was created successfully
  assert.ok(service, 'BackendService should be created successfully');
  assert.strictEqual(service.logger, mockLogger, 'Logger should be assigned correctly');
  
  // No logger calls should happen during instantiation (logger is just stored)
  // This is correct behavior - logger is only used later during operations
});

console.log('\n=== Test Summary ===');
console.log(`Passed: ${passed}`);
console.log(`Failed: ${failed}`);

if (failed > 0) {
  console.log('\n❌ Some tests failed');
  process.exit(1);
} else {
  console.log('\n✅ All tests passed - Logger initialization is working correctly!');
  process.exit(0);
}
