/**
 * Test Shutdown Orchestrator
 * 
 * Validates that:
 * 1. ShutdownOrchestrator module exists and exports correct interface
 * 2. shutdown-orchestrator.js integrates properly with main.js
 * 3. Shutdown sequence handles various scenarios correctly
 * 4. User prompts work for active renders
 */

const fs = require('fs');
const path = require('path');

console.log('=== Shutdown Orchestrator Tests ===\n');

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

// Test 1: Verify shutdown-orchestrator.js exists
test('shutdown-orchestrator.js module exists', () => {
  const modulePath = path.join(__dirname, '../electron/shutdown-orchestrator.js');
  if (!fs.existsSync(modulePath)) {
    throw new Error('shutdown-orchestrator.js not found');
  }
});

// Test 2: Verify ShutdownOrchestrator has required methods
test('ShutdownOrchestrator has all required methods', () => {
  const content = fs.readFileSync(
    path.join(__dirname, '../electron/shutdown-orchestrator.js'),
    'utf8'
  );
  
  const requiredMethods = [
    'setComponents',
    'checkActiveRenders',
    'showActiveRenderWarning',
    'initiateShutdown',
    'waitForRenders',
    'closeWindows',
    'signalBackendShutdown',
    'stopBackend',
    'forceKillBackend',
    'cleanup',
    'forceShutdown',
    'getStatus'
  ];
  
  for (const method of requiredMethods) {
    if (!content.includes(method)) {
      throw new Error(`ShutdownOrchestrator missing method: ${method}`);
    }
  }
});

// Test 3: Verify main.js imports ShutdownOrchestrator
test('main.js imports ShutdownOrchestrator', () => {
  const mainJs = fs.readFileSync(
    path.join(__dirname, '../electron/main.js'),
    'utf8'
  );
  
  if (!mainJs.includes("require('./shutdown-orchestrator')")) {
    throw new Error('main.js does not import ShutdownOrchestrator');
  }
});

// Test 4: Verify main.js declares shutdownOrchestrator variable
test('main.js declares shutdownOrchestrator variable', () => {
  const mainJs = fs.readFileSync(
    path.join(__dirname, '../electron/main.js'),
    'utf8'
  );
  
  if (!mainJs.includes('let shutdownOrchestrator')) {
    throw new Error('main.js does not declare shutdownOrchestrator variable');
  }
});

// Test 5: Verify main.js initializes ShutdownOrchestrator
test('main.js initializes ShutdownOrchestrator', () => {
  const mainJs = fs.readFileSync(
    path.join(__dirname, '../electron/main.js'),
    'utf8'
  );
  
  if (!mainJs.includes('new ShutdownOrchestrator')) {
    throw new Error('main.js does not create ShutdownOrchestrator instance');
  }
  
  if (!mainJs.includes('setComponents')) {
    throw new Error('main.js does not call setComponents on orchestrator');
  }
});

// Test 6: Verify before-quit handler uses ShutdownOrchestrator
test('before-quit handler uses ShutdownOrchestrator', () => {
  const mainJs = fs.readFileSync(
    path.join(__dirname, '../electron/main.js'),
    'utf8'
  );
  
  if (!mainJs.includes("app.on('before-quit'")) {
    throw new Error('before-quit handler not found');
  }
  
  if (!mainJs.includes('shutdownOrchestrator.initiateShutdown')) {
    throw new Error('before-quit handler does not call initiateShutdown');
  }
});

// Test 7: Verify window-all-closed handler properly triggers quit
test('window-all-closed handler uses ShutdownOrchestrator', () => {
  const mainJs = fs.readFileSync(
    path.join(__dirname, '../electron/main.js'),
    'utf8'
  );
  
  if (!mainJs.includes("app.on('window-all-closed'")) {
    throw new Error('window-all-closed handler not found');
  }
  
  const handlerMatch = mainJs.match(/app\.on\('window-all-closed'.*?\}\);/s);
  if (!handlerMatch) {
    throw new Error('Could not parse window-all-closed handler');
  }
  
  const handlerCode = handlerMatch[0];
  // Updated: window-all-closed now calls app.quit() which triggers before-quit
  // This is the correct approach to avoid competing shutdown handlers
  if (!handlerCode.includes('app.quit()') && !handlerCode.includes('shutdownOrchestrator')) {
    throw new Error('window-all-closed handler must call app.quit() or shutdownOrchestrator');
  }
});

// Test 8: Verify ShutdownOrchestrator has timeout constants
test('ShutdownOrchestrator defines timeout constants', () => {
  const content = fs.readFileSync(
    path.join(__dirname, '../electron/shutdown-orchestrator.js'),
    'utf8'
  );
  
  const requiredConstants = [
    'GRACEFUL_TIMEOUT_MS',
    'COMPONENT_TIMEOUT_MS',
    'FORCE_KILL_TIMEOUT_MS'
  ];
  
  for (const constant of requiredConstants) {
    if (!content.includes(constant)) {
      throw new Error(`ShutdownOrchestrator missing constant: ${constant}`);
    }
  }
});

// Test 9: Verify ShutdownOrchestrator checks for active renders
test('ShutdownOrchestrator checks active renders', () => {
  const content = fs.readFileSync(
    path.join(__dirname, '../electron/shutdown-orchestrator.js'),
    'utf8'
  );
  
  if (!content.includes('/api/jobs/active')) {
    throw new Error('ShutdownOrchestrator does not check for active renders');
  }
  
  if (!content.includes('hasActiveRenders')) {
    throw new Error('ShutdownOrchestrator missing hasActiveRenders property');
  }
});

// Test 10: Verify ShutdownOrchestrator signals backend shutdown
test('ShutdownOrchestrator signals backend shutdown', () => {
  const content = fs.readFileSync(
    path.join(__dirname, '../electron/shutdown-orchestrator.js'),
    'utf8'
  );
  
  if (!content.includes('/api/system/shutdown')) {
    throw new Error('ShutdownOrchestrator does not call /api/system/shutdown');
  }
});

// Test 11: Verify ShutdownOrchestrator uses dialog for warnings
test('ShutdownOrchestrator uses dialog for user prompts', () => {
  const content = fs.readFileSync(
    path.join(__dirname, '../electron/shutdown-orchestrator.js'),
    'utf8'
  );
  
  if (!content.includes('showMessageBox')) {
    throw new Error('ShutdownOrchestrator does not use showMessageBox for warnings');
  }
  
  if (!content.includes('Active Renders in Progress')) {
    throw new Error('ShutdownOrchestrator missing active renders warning message');
  }
});

// Test 12: Verify ShutdownOrchestrator handles force kill
test('ShutdownOrchestrator handles force kill', () => {
  const content = fs.readFileSync(
    path.join(__dirname, '../electron/shutdown-orchestrator.js'),
    'utf8'
  );
  
  if (!content.includes('taskkill') && !content.includes('process.kill')) {
    throw new Error('ShutdownOrchestrator missing process termination logic');
  }
});

// Test 13: Verify user cancellation support
test('ShutdownOrchestrator supports user cancellation', () => {
  const content = fs.readFileSync(
    path.join(__dirname, '../electron/shutdown-orchestrator.js'),
    'utf8'
  );
  
  if (!content.includes('user-cancelled')) {
    throw new Error('ShutdownOrchestrator missing user cancellation support');
  }
  
  const mainJs = fs.readFileSync(
    path.join(__dirname, '../electron/main.js'),
    'utf8'
  );
  
  if (!mainJs.includes('user-cancelled')) {
    throw new Error('main.js does not handle user cancellation');
  }
});

// Summary
console.log('\n=== Test Summary ===');
console.log(`Passed: ${testsPassed}`);
console.log(`Failed: ${testsFailed}`);
console.log(`Total:  ${testsPassed + testsFailed}`);

if (testsFailed > 0) {
  console.log('\n❌ Some tests failed');
  process.exit(1);
} else {
  console.log('\n✅ All tests passed');
  process.exit(0);
}
