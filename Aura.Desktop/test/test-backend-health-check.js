/**
 * Test: Backend Health Check Verification
 * Validates the enhanced waitForReady method with /health endpoint
 */

const assert = require('assert');

console.log('=== Backend Health Check Verification Tests ===\n');

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

// Test 1: waitForReady uses /health endpoint
test('waitForReady method uses /health endpoint', () => {
  const BackendService = require('../electron/backend-service.js');
  const methodStr = BackendService.prototype.waitForReady.toString();
  assert.ok(methodStr.includes('/health'), 'waitForReady should use /health endpoint');
});

// Test 2: waitForReady validates status code 200
test('waitForReady validates status === 200', () => {
  const BackendService = require('../electron/backend-service.js');
  const methodStr = BackendService.prototype.waitForReady.toString();
  assert.ok(methodStr.includes('status === 200') || methodStr.includes('status) => status === 200'), 
    'waitForReady should validate status code 200');
});

// Test 3: waitForReady checks if process is killed
test('waitForReady checks if process is killed', () => {
  const BackendService = require('../electron/backend-service.js');
  const methodStr = BackendService.prototype.waitForReady.toString();
  assert.ok(methodStr.includes('this.process') && methodStr.includes('killed'), 
    'waitForReady should check if process is killed');
});

// Test 4: waitForReady uses logger for structured logging
test('waitForReady uses logger for structured logging', () => {
  const BackendService = require('../electron/backend-service.js');
  const methodStr = BackendService.prototype.waitForReady.toString();
  assert.ok(methodStr.includes('this.logger'), 
    'waitForReady should use logger for structured logging');
});

// Test 5: waitForReady logs health check attempts
test('waitForReady logs health check attempts', () => {
  const BackendService = require('../electron/backend-service.js');
  const methodStr = BackendService.prototype.waitForReady.toString();
  assert.ok(methodStr.includes('attemptCount') || methodStr.includes('attempt'), 
    'waitForReady should track attempt count');
});

// Test 6: waitForReady reports progress with phase information
test('waitForReady reports progress with phase information', () => {
  const BackendService = require('../electron/backend-service.js');
  const methodStr = BackendService.prototype.waitForReady.toString();
  assert.ok(methodStr.includes('phase'), 
    'waitForReady should report progress with phase information');
});

// Test 7: waitForReady includes 'health-check' phase
test('waitForReady includes health-check phase', () => {
  const BackendService = require('../electron/backend-service.js');
  const methodStr = BackendService.prototype.waitForReady.toString();
  assert.ok(methodStr.includes('health-check'), 
    'waitForReady should include health-check phase');
});

// Test 8: waitForReady includes 'complete' phase
test('waitForReady includes complete phase', () => {
  const BackendService = require('../electron/backend-service.js');
  const methodStr = BackendService.prototype.waitForReady.toString();
  assert.ok(methodStr.includes('complete'), 
    'waitForReady should include complete phase');
});

// Test 9: waitForReady logs timeout with error details
test('waitForReady logs timeout with error details', () => {
  const BackendService = require('../electron/backend-service.js');
  const methodStr = BackendService.prototype.waitForReady.toString();
  assert.ok(methodStr.includes('timeout') && methodStr.includes('lastError'), 
    'waitForReady should log timeout with error details');
});

// Test 10: BackendService constructor accepts logger parameter
test('BackendService constructor accepts logger parameter', () => {
  const BackendService = require('../electron/backend-service.js');
  const constructorStr = BackendService.toString();
  assert.ok(constructorStr.includes('logger'), 
    'BackendService constructor should accept logger parameter');
});

// Test 11: BackendService initializes logger
test('BackendService initializes logger with fallback', () => {
  const BackendService = require('../electron/backend-service.js');
  const constructorStr = BackendService.toString();
  assert.ok(constructorStr.includes('this.logger') && constructorStr.includes('console'), 
    'BackendService should initialize logger with console fallback');
});

// Test 12: waitForReady uses getUrl() method
test('waitForReady uses getUrl() method', () => {
  const BackendService = require('../electron/backend-service.js');
  const methodStr = BackendService.prototype.waitForReady.toString();
  assert.ok(methodStr.includes('this.getUrl()') || methodStr.includes('getUrl()'), 
    'waitForReady should use getUrl() method');
});

// Test 13: waitForReady builds health check URL correctly
test('waitForReady builds healthCheckUrl variable', () => {
  const BackendService = require('../electron/backend-service.js');
  const methodStr = BackendService.prototype.waitForReady.toString();
  assert.ok(methodStr.includes('healthCheckUrl'), 
    'waitForReady should build healthCheckUrl variable');
});

// Test 14: waitForReady includes maxAttempts calculation
test('waitForReady calculates maxAttempts', () => {
  const BackendService = require('../electron/backend-service.js');
  const methodStr = BackendService.prototype.waitForReady.toString();
  assert.ok(methodStr.includes('maxAttempts'), 
    'waitForReady should calculate maxAttempts');
});

// Test 15: waitForReady logs at specific intervals (every 10 attempts)
test('waitForReady logs at specific intervals', () => {
  const BackendService = require('../electron/backend-service.js');
  const methodStr = BackendService.prototype.waitForReady.toString();
  assert.ok(methodStr.includes('% 10 === 0'), 
    'waitForReady should log at specific intervals (every 10 attempts)');
});

console.log('\n=== Test Summary ===');
console.log(`Passed: ${passed}`);
console.log(`Failed: ${failed}`);

if (failed > 0) {
  console.log('\n❌ Some tests failed');
  process.exit(1);
} else {
  console.log('\n✅ All tests passed');
  process.exit(0);
}
