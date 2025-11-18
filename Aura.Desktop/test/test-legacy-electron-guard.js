#!/usr/bin/env node
/**
 * Test: Legacy electron.js execution guard
 * 
 * This test verifies that the legacy electron.js file has a proper execution
 * guard that prevents it from being used as an entry point.
 */

const { spawn } = require('child_process');
const path = require('path');

console.log('Testing legacy electron.js execution guard...\n');

const electronJsPath = path.join(__dirname, '..', 'electron.js');

// Try to execute electron.js with node (not as an Electron main process)
const nodeProcess = spawn('node', [electronJsPath], {
  stdio: 'pipe',
  cwd: path.join(__dirname, '..')
});

let stdout = '';
let stderr = '';

nodeProcess.stdout.on('data', (data) => {
  stdout += data.toString();
});

nodeProcess.stderr.on('data', (data) => {
  stderr += data.toString();
});

nodeProcess.on('close', (code) => {
  console.log('='.repeat(70));
  console.log('Test Results');
  console.log('='.repeat(70));
  
  // The script should exit with error code 1
  if (code !== 0) {
    console.log('✅ electron.js exited with error code:', code);
  } else {
    console.error('❌ FAIL: electron.js did not exit with error');
    console.error('   Expected non-zero exit code, got:', code);
    process.exit(1);
  }
  
  // Check stderr contains the error message
  if (stderr.includes('CONFIGURATION ERROR') || 
      stderr.includes('Legacy electron.js was executed')) {
    console.log('✅ Error message contains expected warning');
  } else {
    console.error('❌ FAIL: Error message missing expected content');
    console.error('   Expected "CONFIGURATION ERROR" or "Legacy electron.js was executed"');
    console.error('   Got:', stderr.substring(0, 200));
    process.exit(1);
  }
  
  // Check that it mentions electron/main.js as the correct entry
  if (stderr.includes('electron/main.js')) {
    console.log('✅ Error message references correct entry point (electron/main.js)');
  } else {
    console.error('❌ FAIL: Error message does not reference electron/main.js');
    process.exit(1);
  }
  
  // Check the error mentions package.json
  if (stderr.includes('package.json')) {
    console.log('✅ Error message mentions package.json configuration');
  } else {
    console.warn('⚠️  WARNING: Error message does not mention package.json');
  }
  
  console.log('\n' + '='.repeat(70));
  console.log('Test Summary');
  console.log('='.repeat(70));
  console.log('✅ All checks passed!');
  console.log('   The legacy electron.js file properly prevents execution');
  console.log('   and provides clear guidance to fix the configuration.');
  console.log('\nSample error output:');
  console.log('-'.repeat(70));
  console.log(stderr.substring(0, 500));
  console.log('-'.repeat(70));
  
  process.exit(0);
});
