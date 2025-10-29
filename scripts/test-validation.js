#!/usr/bin/env node

/**
 * Test runner for all build validation scripts
 * Runs all test suites and reports overall results
 */

import { execSync } from 'child_process';
import { fileURLToPath } from 'url';
import { dirname, join } from 'path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const testFiles = [
  join(__dirname, 'build', 'validate-environment.test.js'),
  join(__dirname, 'build', 'verify-build.test.js'),
  join(__dirname, 'audit', 'find-placeholders.test.js')
];

let totalPassed = 0;
let totalFailed = 0;

console.log('\n========================================');
console.log('  Build Validation Test Suite Runner');
console.log('========================================\n');

for (const testFile of testFiles) {
  const testName = testFile.split('/').slice(-2).join('/');
  console.log(`Running: ${testName}`);
  console.log('-'.repeat(50));
  
  try {
    const output = execSync(`node "${testFile}"`, { 
      encoding: 'utf8',
      stdio: 'inherit'
    });
    totalPassed++;
  } catch (error) {
    totalFailed++;
    console.log(`\n⚠️  Test suite failed: ${testName}\n`);
  }
}

console.log('\n========================================');
console.log('  Overall Test Results');
console.log('========================================\n');

console.log(`Test Suites Passed: ${totalPassed}/${testFiles.length}`);
console.log(`Test Suites Failed: ${totalFailed}/${testFiles.length}`);
console.log('');

if (totalFailed > 0) {
  console.log('❌ Some test suites failed. Please review the output above.');
  process.exit(1);
} else {
  console.log('✅ All test suites passed!');
  process.exit(0);
}
