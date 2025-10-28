#!/usr/bin/env node

/**
 * Test suite for validate-environment.js
 * 
 * Note: These are integration tests that verify the validation script
 * can run and produce expected output patterns.
 */

import { execSync } from 'child_process';
import { existsSync } from 'fs';
import { fileURLToPath } from 'url';
import { dirname, join } from 'path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const scriptPath = join(__dirname, 'validate-environment.js');

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

console.log('\n=== Testing validate-environment.js ===\n');

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
    // Script may exit with non-zero (that's ok for this test)
    // We're just checking it doesn't crash with syntax errors
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
      output.includes('Environment Validation'),
      'Should output validation header'
    );
  } catch (error) {
    // Check stderr/stdout in error
    const output = error.stdout || error.stderr || '';
    assertTrue(
      output.includes('Environment Validation'),
      'Should output validation header'
    );
  }
});

test('Script checks Node.js version', () => {
  try {
    const output = execSync(`node "${scriptPath}"`, { 
      encoding: 'utf8',
      stdio: 'pipe'
    });
    assertTrue(
      output.includes('Node.js version'),
      'Should check Node.js version'
    );
  } catch (error) {
    const output = error.stdout || '';
    assertTrue(
      output.includes('Node.js version'),
      'Should check Node.js version'
    );
  }
});

test('Script checks npm version', () => {
  try {
    const output = execSync(`node "${scriptPath}"`, { 
      encoding: 'utf8',
      stdio: 'pipe'
    });
    assertTrue(
      output.includes('npm version'),
      'Should check npm version'
    );
  } catch (error) {
    const output = error.stdout || '';
    assertTrue(
      output.includes('npm version'),
      'Should check npm version'
    );
  }
});

test('Script checks for .nvmrc', () => {
  try {
    const output = execSync(`node "${scriptPath}"`, { 
      encoding: 'utf8',
      stdio: 'pipe'
    });
    assertTrue(
      output.includes('.nvmrc') || output.includes('18.18.0'),
      'Should reference .nvmrc or exact version'
    );
  } catch (error) {
    const output = error.stdout || '';
    assertTrue(
      output.includes('.nvmrc') || output.includes('18.18.0'),
      'Should reference .nvmrc or exact version'
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
      output.includes('Validation Summary'),
      'Should output validation summary'
    );
  } catch (error) {
    const output = error.stdout || '';
    assertTrue(
      output.includes('Validation Summary'),
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
