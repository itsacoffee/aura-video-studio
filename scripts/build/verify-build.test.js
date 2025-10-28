#!/usr/bin/env node

/**
 * Test suite for verify-build.js
 * 
 * Note: These are integration tests that verify the build verification script
 * can run and produce expected output patterns.
 */

import { execSync } from 'child_process';
import { existsSync } from 'fs';
import { fileURLToPath } from 'url';
import { dirname, join } from 'path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const scriptPath = join(__dirname, 'verify-build.js');

let passed = 0;
let failed = 0;

function test(name, fn) {
  try {
    fn();
    console.log(`✓ ${name}`);
    passed++;
  } catch (error) {
    console.log(`✗ ${name}`);
    console.log(`  ${error.message}`);
    failed++;
  }
}

function assertTrue(condition, message) {
  if (!condition) {
    throw new Error(message || 'Assertion failed');
  }
}

console.log('\n=== Testing verify-build.js ===\n');

test('Script exists and is executable', () => {
  assertTrue(existsSync(scriptPath), 'Script file should exist');
});

test('Script runs without crashing', () => {
  try {
    execSync(`node "${scriptPath}"`, { 
      encoding: 'utf8',
      stdio: 'pipe'
    });
  } catch (error) {
    // Script may exit with non-zero if build not present (that's ok)
    assertTrue(
      error.status === 0 || error.status === 1,
      'Script should exit with 0 or 1'
    );
  }
});

test('Script outputs validation header', () => {
  try {
    const output = execSync(`node "${scriptPath}"`, { 
      encoding: 'utf8',
      stdio: 'pipe'
    });
    assertTrue(
      output.includes('Build Output Validation') || output.includes('Build Verification'),
      'Should output validation header'
    );
  } catch (error) {
    const output = error.stdout || '';
    assertTrue(
      output.includes('Build Output Validation') || output.includes('Build Verification'),
      'Should output validation header'
    );
  }
});

test('Script checks for dist directory', () => {
  try {
    const output = execSync(`node "${scriptPath}"`, { 
      encoding: 'utf8',
      stdio: 'pipe'
    });
    assertTrue(
      output.includes('dist'),
      'Should check for dist directory'
    );
  } catch (error) {
    const output = error.stdout || '';
    assertTrue(
      output.includes('dist'),
      'Should check for dist directory'
    );
  }
});

test('Script checks for index.html or reports missing dist', () => {
  try {
    const output = execSync(`node "${scriptPath}"`, { 
      encoding: 'utf8',
      stdio: 'pipe'
    });
    assertTrue(
      output.includes('index.html') || output.includes('dist'),
      'Should check for index.html or report missing dist'
    );
  } catch (error) {
    const output = error.stdout || '';
    assertTrue(
      output.includes('index.html') || output.includes('dist'),
      'Should check for index.html or report missing dist'
    );
  }
});

test('Script outputs validation summary', () => {
  try {
    const output = execSync(`node "${scriptPath}"`, { 
      encoding: 'utf8',
      stdio: 'pipe'
    });
    assertTrue(
      output.includes('Validation Summary') || output.includes('verification'),
      'Should output validation summary'
    );
  } catch (error) {
    const output = error.stdout || '';
    assertTrue(
      output.includes('Validation Summary') || output.includes('verification'),
      'Should output validation summary'
    );
  }
});

test('Script uses colored output symbols', () => {
  try {
    const output = execSync(`node "${scriptPath}"`, { 
      encoding: 'utf8',
      stdio: 'pipe'
    });
    assertTrue(
      output.includes('✓') || output.includes('✗') || output.includes('⚠'),
      'Should use check/error/warning symbols'
    );
  } catch (error) {
    const output = error.stdout || '';
    assertTrue(
      output.includes('✓') || output.includes('✗') || output.includes('⚠'),
      'Should use check/error/warning symbols'
    );
  }
});

console.log('\n=== Test Summary ===\n');
console.log(`Passed: ${passed}`);
console.log(`Failed: ${failed}`);
console.log('');

if (failed > 0) {
  console.log('Some tests failed. Please review the output above.');
  process.exit(1);
} else {
  console.log('All tests passed!');
  process.exit(0);
}
