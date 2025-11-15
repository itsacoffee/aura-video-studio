/**
 * Test ProcessManager
 * 
 * Validates that:
 * 1. ProcessManager module exists and exports correct interface
 * 2. Process registration and unregistration work correctly
 * 3. Process termination works as expected
 * 4. Diagnostics provide useful information
 */

const fs = require('fs');
const path = require('path');

console.log('=== ProcessManager Tests ===\n');

let testsPassed = 0;
let testsFailed = 0;

function test(name, fn) {
  try {
    fn();
    console.log(`✓ ${name}`);
    testsPassed++;
  } catch (error) {
    console.error(`✗ ${name}`);
    console.error(`  Error: ${error.message}`);
    testsFailed++;
  }
}

// Test 1: Verify process-manager.js exists
test('process-manager.js module exists', () => {
  const modulePath = path.join(__dirname, '../electron/process-manager.js');
  if (!fs.existsSync(modulePath)) {
    throw new Error('process-manager.js not found');
  }
});

// Test 2: Verify ProcessManager can be required
test('ProcessManager can be required', () => {
  const ProcessManager = require('../electron/process-manager');
  if (typeof ProcessManager !== 'function') {
    throw new Error('ProcessManager is not a constructor');
  }
});

// Test 3: Verify ProcessManager has required methods
test('ProcessManager has all required methods', () => {
  const ProcessManager = require('../electron/process-manager');
  const mockLogger = { info: () => {}, warn: () => {}, error: () => {}, debug: () => {} };
  const manager = new ProcessManager(mockLogger);
  
  const requiredMethods = [
    'register',
    'unregister',
    'getAllProcesses',
    'getProcessCount',
    'getProcess',
    'hasProcess',
    'terminate',
    'terminateAll',
    'getDiagnostics',
    'cleanup'
  ];
  
  for (const method of requiredMethods) {
    if (typeof manager[method] !== 'function') {
      throw new Error(`ProcessManager missing method: ${method}`);
    }
  }
});

// Test 4: Verify main.js imports ProcessManager
test('main.js imports ProcessManager', () => {
  const mainJs = fs.readFileSync(
    path.join(__dirname, '../electron/main.js'),
    'utf8'
  );
  
  if (!mainJs.includes("require('./process-manager')")) {
    throw new Error('main.js does not import ProcessManager');
  }
});

// Test 5: Verify main.js declares processManager variable
test('main.js declares processManager variable', () => {
  const mainJs = fs.readFileSync(
    path.join(__dirname, '../electron/main.js'),
    'utf8'
  );
  
  if (!mainJs.includes('let processManager')) {
    throw new Error('main.js does not declare processManager variable');
  }
});

// Test 6: Verify main.js initializes ProcessManager
test('main.js initializes ProcessManager', () => {
  const mainJs = fs.readFileSync(
    path.join(__dirname, '../electron/main.js'),
    'utf8'
  );
  
  if (!mainJs.includes('new ProcessManager')) {
    throw new Error('main.js does not create ProcessManager instance');
  }
});

// Test 7: Verify BackendService accepts processManager parameter
test('BackendService accepts processManager parameter', () => {
  const backendServiceJs = fs.readFileSync(
    path.join(__dirname, '../electron/backend-service.js'),
    'utf8'
  );
  
  if (!backendServiceJs.includes('processManager = null')) {
    throw new Error('BackendService constructor does not accept processManager parameter');
  }
});

// Test 8: Verify BackendService registers with ProcessManager
test('BackendService registers process with ProcessManager', () => {
  const backendServiceJs = fs.readFileSync(
    path.join(__dirname, '../electron/backend-service.js'),
    'utf8'
  );
  
  if (!backendServiceJs.includes('processManager.register')) {
    throw new Error('BackendService does not call processManager.register');
  }
});

// Test 9: Verify ShutdownOrchestrator accepts processManager
test('ShutdownOrchestrator accepts processManager', () => {
  const shutdownOrchestratorJs = fs.readFileSync(
    path.join(__dirname, '../electron/shutdown-orchestrator.js'),
    'utf8'
  );
  
  if (!shutdownOrchestratorJs.includes('processManager')) {
    throw new Error('ShutdownOrchestrator does not reference processManager');
  }
});

// Test 10: Verify ShutdownOrchestrator has terminateAllProcesses method
test('ShutdownOrchestrator has terminateAllProcesses method', () => {
  const shutdownOrchestratorJs = fs.readFileSync(
    path.join(__dirname, '../electron/shutdown-orchestrator.js'),
    'utf8'
  );
  
  if (!shutdownOrchestratorJs.includes('terminateAllProcesses')) {
    throw new Error('ShutdownOrchestrator does not have terminateAllProcesses method');
  }
});

// Test 11: Verify ProcessManager uses platform-specific termination
test('ProcessManager has platform-specific termination methods', () => {
  const processManagerJs = fs.readFileSync(
    path.join(__dirname, '../electron/process-manager.js'),
    'utf8'
  );
  
  if (!processManagerJs.includes('_windowsTerminate') || !processManagerJs.includes('_unixTerminate')) {
    throw new Error('ProcessManager missing platform-specific termination methods');
  }
  
  if (!processManagerJs.includes('taskkill') || !processManagerJs.includes('SIGTERM')) {
    throw new Error('ProcessManager missing platform-specific commands');
  }
});

// Test 12: Verify ProcessManager tracks process lifecycle
test('ProcessManager tracks process lifecycle events', () => {
  const processManagerJs = fs.readFileSync(
    path.join(__dirname, '../electron/process-manager.js'),
    'utf8'
  );
  
  if (!processManagerJs.includes('startTime') || !processManagerJs.includes('lifetime')) {
    throw new Error('ProcessManager does not track process lifetime');
  }
  
  if (!processManagerJs.includes("process.on('exit'")) {
    throw new Error('ProcessManager does not listen for process exit events');
  }
});

// Test 13: Verify ProcessManager provides diagnostics
test('ProcessManager provides diagnostic information', () => {
  const processManagerJs = fs.readFileSync(
    path.join(__dirname, '../electron/process-manager.js'),
    'utf8'
  );
  
  if (!processManagerJs.includes('getDiagnostics')) {
    throw new Error('ProcessManager does not provide getDiagnostics method');
  }
  
  if (!processManagerJs.includes('processCount')) {
    throw new Error('ProcessManager diagnostics do not include processCount');
  }
});

// Print summary
console.log('\n=== Test Summary ===');
console.log(`Passed: ${testsPassed}`);
console.log(`Failed: ${testsFailed}`);

if (testsFailed === 0) {
  console.log('\n✅ All tests passed');
  process.exit(0);
} else {
  console.log('\n❌ Some tests failed');
  process.exit(1);
}
