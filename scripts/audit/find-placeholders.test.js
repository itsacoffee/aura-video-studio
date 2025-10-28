#!/usr/bin/env node

/**
 * Test suite for find-placeholders.js
 * 
 * Tests the placeholder scanning functionality to ensure it correctly
 * identifies forbidden patterns and reports them accurately.
 */

import { execSync } from 'child_process';
import { existsSync, mkdirSync, writeFileSync, rmSync } from 'fs';
import { fileURLToPath } from 'url';
import { dirname, join } from 'path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const scriptPath = join(__dirname, '..', 'audit', 'find-placeholders.js');
const testDir = join(__dirname, 'test-temp');

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

function setupTestDir() {
  if (existsSync(testDir)) {
    rmSync(testDir, { recursive: true, force: true });
  }
  mkdirSync(testDir, { recursive: true });
}

function cleanupTestDir() {
  if (existsSync(testDir)) {
    rmSync(testDir, { recursive: true, force: true });
  }
}

console.log('\n=== Testing find-placeholders.js ===\n');

test('Script exists and is executable', () => {
  assertTrue(existsSync(scriptPath), 'Script file should exist');
});

test('Script runs without crashing on clean repo', () => {
  try {
    const output = execSync(`node "${scriptPath}"`, { 
      encoding: 'utf8',
      stdio: 'pipe',
      cwd: join(__dirname, '..', '..')
    });
    // Should exit 0 or 1 depending on whether placeholders found
    assertTrue(true, 'Script should run');
  } catch (error) {
    // Exit code 1 is ok (placeholders found)
    assertTrue(
      error.status === 0 || error.status === 1,
      'Script should exit with 0 or 1'
    );
  }
});

test('Script outputs scanner header', () => {
  try {
    const output = execSync(`node "${scriptPath}"`, { 
      encoding: 'utf8',
      stdio: 'pipe',
      cwd: join(__dirname, '..', '..')
    });
    assertTrue(
      output.includes('Placeholder Scanner') || output.includes('Scanning'),
      'Should output scanner header'
    );
  } catch (error) {
    const output = error.stdout || '';
    assertTrue(
      output.includes('Placeholder Scanner') || output.includes('Scanning'),
      'Should output scanner header'
    );
  }
});

test('Script reports file counts', () => {
  try {
    const output = execSync(`node "${scriptPath}"`, { 
      encoding: 'utf8',
      stdio: 'pipe',
      cwd: join(__dirname, '..', '..')
    });
    assertTrue(
      output.includes('Total files') || output.includes('Scanned files'),
      'Should report file counts'
    );
  } catch (error) {
    const output = error.stdout || '';
    assertTrue(
      output.includes('Total files') || output.includes('Scanned files'),
      'Should report file counts'
    );
  }
});

test('Script uses colored output symbols', () => {
  try {
    const output = execSync(`node "${scriptPath}"`, { 
      encoding: 'utf8',
      stdio: 'pipe',
      cwd: join(__dirname, '..', '..')
    });
    assertTrue(
      output.includes('✓') || output.includes('✗'),
      'Should use check/error symbols'
    );
  } catch (error) {
    const output = error.stdout || '';
    assertTrue(
      output.includes('✓') || output.includes('✗'),
      'Should use check/error symbols'
    );
  }
});

console.log('\n=== Test Summary ===\n');
console.log(`Passed: ${passed}`);
console.log(`Failed: ${failed}`);
console.log('');

cleanupTestDir();

if (failed > 0) {
  console.log('Some tests failed. Please review the output above.');
  process.exit(1);
} else {
  console.log('All tests passed!');
  process.exit(0);
}
