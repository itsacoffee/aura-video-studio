/**
 * Test Electron Backend Integration
 * 
 * Validates that:
 * 1. BackendService is properly integrated into electron.js
 * 2. IPC handlers are correctly wired
 * 3. Backend can start and stop properly
 * 4. Preload script exposes backend APIs
 */

const fs = require('fs');
const path = require('path');

console.log('=== Electron Backend Integration Tests ===\n');

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

// Test 1: Verify electron.js imports BackendService
test('electron.js imports BackendService module', () => {
  const electronJs = fs.readFileSync(
    path.join(__dirname, '../electron.js'),
    'utf8'
  );
  
  if (!electronJs.includes("require('./electron/backend-service')")) {
    throw new Error('electron.js does not import BackendService');
  }
  
  if (electronJs.includes('spawn') && electronJs.includes('findAvailablePort')) {
    throw new Error('electron.js still has inline backend management code - should use BackendService');
  }
});

// Test 2: Verify backend-service.js exists and has required methods
test('backend-service.js has all required methods', () => {
  const backendService = fs.readFileSync(
    path.join(__dirname, '../electron/backend-service.js'),
    'utf8'
  );
  
  const requiredMethods = [
    'start',
    'stop',
    'restart',
    'getPort',
    'getUrl',
    'isRunning',
    'checkFirewallCompatibility',
    'getFirewallRuleStatus',
    'getFirewallRuleCommand'
  ];
  
  for (const method of requiredMethods) {
    if (!backendService.includes(method)) {
      throw new Error(`BackendService missing method: ${method}`);
    }
  }
});

// Test 3: Verify electron.js uses backendService instance methods
test('electron.js uses backendService instance methods', () => {
  const electronJs = fs.readFileSync(
    path.join(__dirname, '../electron.js'),
    'utf8'
  );
  
  const requiredCalls = [
    'backendService.start',
    'backendService.stop',
    'backendService.getPort',
    'backendService.getUrl'
  ];
  
  for (const call of requiredCalls) {
    if (!electronJs.includes(call)) {
      throw new Error(`electron.js missing call to: ${call}`);
    }
  }
});

// Test 4: Verify IPC handlers are properly wired
test('IPC handlers use backendService', () => {
  const electronJs = fs.readFileSync(
    path.join(__dirname, '../electron.js'),
    'utf8'
  );
  
  const requiredHandlers = [
    "ipcMain.handle('backend:getUrl'",
    "ipcMain.handle('backend:status'",
    "ipcMain.handle('backend:restart'",
    "ipcMain.handle('backend:checkFirewall'"
  ];
  
  for (const handler of requiredHandlers) {
    if (!electronJs.includes(handler)) {
      throw new Error(`Missing IPC handler: ${handler}`);
    }
  }
});

// Test 5: Verify preload.js exposes backend APIs
test('preload.js exposes backend APIs via contextBridge', () => {
  const preloadJs = fs.readFileSync(
    path.join(__dirname, '../electron/preload.js'),
    'utf8'
  );
  
  if (!preloadJs.includes('contextBridge.exposeInMainWorld')) {
    throw new Error('preload.js does not use contextBridge');
  }
  
  const requiredApis = [
    'getUrl:',
    'status:',
    'restart:',
    'checkFirewall:'
  ];
  
  for (const api of requiredApis) {
    if (!preloadJs.includes(api)) {
      throw new Error(`preload.js missing API: ${api}`);
    }
  }
});

// Test 6: Verify package.json has coordinated scripts
test('package.json has coordinated build scripts', () => {
  const packageJson = JSON.parse(
    fs.readFileSync(path.join(__dirname, '../package.json'), 'utf8')
  );
  
  const requiredScripts = [
    'electron:dev',
    'electron:build',
    'backend:build',
    'backend:build:dev',
    'frontend:build',
    'build:all',
    'prebuild:check'
  ];
  
  for (const script of requiredScripts) {
    if (!packageJson.scripts[script]) {
      throw new Error(`Missing package.json script: ${script}`);
    }
  }
});

// Test 7: Verify cleanup uses async/await
test('electron.js cleanup is async', () => {
  const electronJs = fs.readFileSync(
    path.join(__dirname, '../electron.js'),
    'utf8'
  );
  
  if (!electronJs.includes('async function cleanup')) {
    throw new Error('cleanup function should be async');
  }
  
  if (!electronJs.includes('await backendService.stop')) {
    throw new Error('cleanup should await backendService.stop()');
  }
});

// Test 8: Verify no hardcoded backend ports
test('No hardcoded backend ports in electron.js', () => {
  const electronJs = fs.readFileSync(
    path.join(__dirname, '../electron.js'),
    'utf8'
  );
  
  // Should not have direct references to port 5000 or 5005 in startup code
  const problematicPatterns = [
    /backendPort\s*=\s*5000/,
    /backendPort\s*=\s*5005/,
    /localhost:5000[^0-9]/,
    /localhost:5005[^0-9]/
  ];
  
  for (const pattern of problematicPatterns) {
    if (pattern.test(electronJs)) {
      // Check if it's in a comment or string template
      const lines = electronJs.split('\n');
      for (const line of lines) {
        if (pattern.test(line) && !line.trim().startsWith('//') && !line.includes('VITE_')) {
          throw new Error(`Found hardcoded port in: ${line.trim()}`);
        }
      }
    }
  }
});

// Test 9: Verify BackendService handles both dev and prod paths
test('BackendService handles dev and prod backend paths', () => {
  const backendService = fs.readFileSync(
    path.join(__dirname, '../electron/backend-service.js'),
    'utf8'
  );
  
  if (!backendService.includes('this.isDev')) {
    throw new Error('BackendService should check isDev flag');
  }
  
  if (!backendService.includes('Aura.Api/bin/Debug')) {
    throw new Error('BackendService missing dev backend path');
  }
  
  if (!backendService.includes('process.resourcesPath')) {
    throw new Error('BackendService missing production backend path');
  }
});

// Test 10: Verify proper error handling in startBackend
test('startBackend has proper error handling', () => {
  const electronJs = fs.readFileSync(
    path.join(__dirname, '../electron.js'),
    'utf8'
  );
  
  if (!electronJs.includes('try {') || !electronJs.includes('catch (error)')) {
    throw new Error('startBackend missing try-catch');
  }
  
  if (!electronJs.includes('dialog.showErrorBox')) {
    throw new Error('startBackend should show error dialog on failure');
  }
});

// Print summary
console.log('\n=== Test Summary ===');
console.log(`Passed: ${testsPassed}`);
console.log(`Failed: ${testsFailed}`);

if (testsFailed > 0) {
  console.log('\n❌ Some tests failed');
  process.exit(1);
} else {
  console.log('\n✅ All tests passed');
  process.exit(0);
}
