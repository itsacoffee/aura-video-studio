/**
 * Test: Backend waitForReady method
 * Validates that the waitForReady method exists and has proper signature
 */

const assert = require('assert');
const path = require('path');

console.log('=== Backend waitForReady Method Tests ===\n');

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

// Test 1: Check if BackendService module can be loaded
test('BackendService module loads without errors', () => {
  const BackendService = require('../electron/backend-service.js');
  assert.ok(BackendService, 'BackendService should be defined');
});

// Test 2: Check if waitForReady method exists
test('BackendService has waitForReady method', () => {
  const BackendService = require('../electron/backend-service.js');
  assert.ok(BackendService.prototype.waitForReady, 'waitForReady method should exist');
  assert.strictEqual(typeof BackendService.prototype.waitForReady, 'function', 'waitForReady should be a function');
});

// Test 3: Check method signature (accepts options parameter)
test('waitForReady method accepts options parameter', () => {
  const BackendService = require('../electron/backend-service.js');
  const methodStr = BackendService.prototype.waitForReady.toString();
  assert.ok(methodStr.includes('options'), 'waitForReady should accept options parameter');
});

// Test 4: Check that waitForReady is async
test('waitForReady is an async method', () => {
  const BackendService = require('../electron/backend-service.js');
  const methodStr = BackendService.prototype.waitForReady.toString();
  assert.ok(methodStr.includes('async'), 'waitForReady should be async');
});

// Test 5: Check default timeout value
test('waitForReady has default timeout configuration', () => {
  const BackendService = require('../electron/backend-service.js');
  const methodStr = BackendService.prototype.waitForReady.toString();
  assert.ok(methodStr.includes('timeout'), 'waitForReady should handle timeout configuration');
  assert.ok(methodStr.includes('90000'), 'Default timeout should be 90000ms');
});

// Test 6: Check onProgress callback handling
test('waitForReady supports onProgress callback', () => {
  const BackendService = require('../electron/backend-service.js');
  const methodStr = BackendService.prototype.waitForReady.toString();
  assert.ok(methodStr.includes('onProgress'), 'waitForReady should support onProgress callback');
});

// Test 7: Check health endpoint usage
test('waitForReady checks health endpoints', () => {
  const BackendService = require('../electron/backend-service.js');
  const methodStr = BackendService.prototype.waitForReady.toString();
  assert.ok(methodStr.includes('health'), 'waitForReady should check health endpoints');
  assert.ok(methodStr.includes('live') || methodStr.includes('ready'), 'waitForReady should check live or ready endpoints');
});

// Test 8: Splash screen HTML file exists
test('splash.html file exists in electron directory', () => {
  const fs = require('fs');
  const splashPath = path.join(__dirname, '../electron/splash.html');
  assert.ok(fs.existsSync(splashPath), 'splash.html should exist');
});

// Test 9: Splash screen has progress bar
test('splash.html contains progress bar elements', () => {
  const fs = require('fs');
  const splashPath = path.join(__dirname, '../electron/splash.html');
  const content = fs.readFileSync(splashPath, 'utf8');
  assert.ok(content.includes('progress-bar'), 'splash.html should have progress bar');
  assert.ok(content.includes('progress-fill'), 'splash.html should have progress fill');
  assert.ok(content.includes('status-message'), 'splash.html should have status message');
});

// Test 10: Splash screen listens for IPC messages
test('splash.html listens for status-update IPC messages', () => {
  const fs = require('fs');
  const splashPath = path.join(__dirname, '../electron/splash.html');
  const content = fs.readFileSync(splashPath, 'utf8');
  assert.ok(content.includes('ipcRenderer'), 'splash.html should use ipcRenderer');
  assert.ok(content.includes('status-update'), 'splash.html should listen for status-update messages');
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
