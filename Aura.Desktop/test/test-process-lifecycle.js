#!/usr/bin/env node
/**
 * Process Lifecycle Test
 * Tests the zombie process elimination implementation
 */

const path = require('path');
const fs = require('fs');

// Test configuration
const testResults = {
  passed: 0,
  failed: 0,
  tests: []
};

function test(name, fn) {
  try {
    fn();
    testResults.passed++;
    testResults.tests.push({ name, status: 'PASS' });
    console.log(`✓ ${name}`);
  } catch (error) {
    testResults.failed++;
    testResults.tests.push({ name, status: 'FAIL', error: error.message });
    console.log(`✗ ${name}`);
    console.log(`  Error: ${error.message}`);
  }
}

console.log('=== Process Lifecycle Tests ===\n');

// Test 1: BackendService has required methods
test('BackendService has stop() method', () => {
  const backendServiceCode = fs.readFileSync(
    path.join(__dirname, '../electron/backend-service.js'),
    'utf8'
  );
  
  if (!backendServiceCode.includes('async stop()')) {
    throw new Error('BackendService missing async stop() method');
  }
});

// Test 2: BackendService has _waitForExit helper
test('BackendService has _waitForExit() helper', () => {
  const backendServiceCode = fs.readFileSync(
    path.join(__dirname, '../electron/backend-service.js'),
    'utf8'
  );
  
  if (!backendServiceCode.includes('_waitForExit(timeoutMs)')) {
    throw new Error('BackendService missing _waitForExit() helper');
  }
});

// Test 3: BackendService tracks backendProcess
test('BackendService tracks backendProcess property', () => {
  const backendServiceCode = fs.readFileSync(
    path.join(__dirname, '../electron/backend-service.js'),
    'utf8'
  );
  
  if (!backendServiceCode.includes('this.backendProcess')) {
    throw new Error('BackendService missing backendProcess property');
  }
});

// Test 4: BackendService orphan cleanup logs summary
test('BackendService orphan cleanup logs summary', () => {
  const backendServiceCode = fs.readFileSync(
    path.join(__dirname, '../electron/backend-service.js'),
    'utf8'
  );
  
  if (!backendServiceCode.includes('Orphan cleanup:')) {
    throw new Error('BackendService orphan cleanup missing summary logging');
  }
  
  if (!backendServiceCode.includes('found') && 
      !backendServiceCode.includes('terminated') && 
      !backendServiceCode.includes('failed')) {
    throw new Error('BackendService orphan cleanup missing detailed counts');
  }
});

// Test 5: BackendService has safety guards in orphan detection
test('BackendService orphan detection has safety guards', () => {
  const backendServiceCode = fs.readFileSync(
    path.join(__dirname, '../electron/backend-service.js'),
    'utf8'
  );
  
  // Check for explicit process name matching
  if (!backendServiceCode.includes('Aura.Api.exe') && 
      !backendServiceCode.includes('Aura.Api')) {
    throw new Error('BackendService orphan detection missing specific process name matching');
  }
  
  // Check for SAFETY comments
  if (!backendServiceCode.includes('SAFETY:')) {
    throw new Error('BackendService orphan detection missing safety documentation');
  }
});

// Test 6: FFmpegHandler has stop() method
test('FFmpegHandler has stop() method', () => {
  const ffmpegHandlerCode = fs.readFileSync(
    path.join(__dirname, '../electron/ipc-handlers/ffmpeg-handler.js'),
    'utf8'
  );
  
  if (!ffmpegHandlerCode.includes('async stop()')) {
    throw new Error('FFmpegHandler missing async stop() method');
  }
});

// Test 7: FFmpegHandler tracks ffmpegProcesses
test('FFmpegHandler tracks ffmpegProcesses Set', () => {
  const ffmpegHandlerCode = fs.readFileSync(
    path.join(__dirname, '../electron/ipc-handlers/ffmpeg-handler.js'),
    'utf8'
  );
  
  if (!ffmpegHandlerCode.includes('this.ffmpegProcesses = new Set()')) {
    throw new Error('FFmpegHandler missing ffmpegProcesses Set initialization');
  }
});

// Test 8: FFmpegHandler has trackProcess() method
test('FFmpegHandler has trackProcess() method', () => {
  const ffmpegHandlerCode = fs.readFileSync(
    path.join(__dirname, '../electron/ipc-handlers/ffmpeg-handler.js'),
    'utf8'
  );
  
  if (!ffmpegHandlerCode.includes('trackProcess(process)')) {
    throw new Error('FFmpegHandler missing trackProcess() method');
  }
});

// Test 9: main.js calls FFmpegHandler.stop() in cleanup
test('main.js calls FFmpegHandler.stop() in cleanup', () => {
  const mainCode = fs.readFileSync(
    path.join(__dirname, '../electron/main.js'),
    'utf8'
  );
  
  if (!mainCode.includes('ipcHandlers.ffmpeg') || 
      !mainCode.includes('.stop()')) {
    throw new Error('main.js cleanup missing FFmpegHandler.stop() call');
  }
});

// Test 10: main.js cleanup calls FFmpeg before backend
test('main.js cleanup calls FFmpeg stop before backend stop', () => {
  const mainCode = fs.readFileSync(
    path.join(__dirname, '../electron/main.js'),
    'utf8'
  );
  
  const ffmpegStopIndex = mainCode.indexOf('ipcHandlers.ffmpeg');
  const backendStopIndex = mainCode.indexOf('backendService.stop()');
  
  if (ffmpegStopIndex === -1 || backendStopIndex === -1) {
    throw new Error('main.js cleanup missing FFmpeg or backend stop calls');
  }
  
  if (ffmpegStopIndex > backendStopIndex) {
    throw new Error('main.js cleanup calls backend stop before FFmpeg stop (wrong order)');
  }
});

// Test 11: BackendService stop() uses SIGINT first
test('BackendService stop() uses SIGINT before SIGKILL', () => {
  const backendServiceCode = fs.readFileSync(
    path.join(__dirname, '../electron/backend-service.js'),
    'utf8'
  );
  
  const sigintIndex = backendServiceCode.indexOf('kill("SIGINT")');
  const sigkillIndex = backendServiceCode.indexOf('kill("SIGKILL")');
  
  if (sigintIndex === -1) {
    throw new Error('BackendService stop() missing SIGINT attempt');
  }
  
  if (sigkillIndex === -1) {
    throw new Error('BackendService stop() missing SIGKILL fallback');
  }
  
  if (sigintIndex > sigkillIndex) {
    throw new Error('BackendService stop() uses SIGKILL before SIGINT (wrong order)');
  }
});

// Test 12: Documentation exists
test('Process lifecycle testing documentation exists', () => {
  const docPath = path.join(__dirname, '../../docs/archive/historical/PROCESS_LIFECYCLE_TESTS.md');
  
  if (!fs.existsSync(docPath)) {
    throw new Error('PROCESS_LIFECYCLE_TESTS.md documentation missing');
  }
  
  const docContent = fs.readFileSync(docPath, 'utf8');
  
  // Check for key sections
  const requiredSections = [
    'Test 1: Normal Exit via Window Close',
    'Test 2: Normal Exit via Menu',
    'Test 3: Exit During Video Rendering',
    'Test 4: Orphan Cleanup on Next Startup',
    'Diagnostics and Troubleshooting'
  ];
  
  for (const section of requiredSections) {
    if (!docContent.includes(section)) {
      throw new Error(`Documentation missing section: ${section}`);
    }
  }
});

// Summary
console.log('\n=== Test Summary ===');
console.log(`Passed: ${testResults.passed}`);
console.log(`Failed: ${testResults.failed}`);
console.log(`Total:  ${testResults.passed + testResults.failed}`);

if (testResults.failed > 0) {
  console.log('\n❌ Some tests failed');
  process.exit(1);
} else {
  console.log('\n✅ All tests passed');
  process.exit(0);
}
