#!/usr/bin/env node
/**
 * Test: Build Backend Script Path Resolution
 * Verifies that build-backend.ps1 correctly resolves paths when called from different directories
 */

const { execSync } = require('child_process');
const path = require('path');
const fs = require('fs');

console.log('Testing build-backend.ps1 path resolution...\n');

// Test 1: Verify script can find project file from Desktop directory
console.log('Test 1: Calling script from Desktop directory with ProjectRoot parameter');
try {
  const result = execSync(
    'pwsh -File ../build-backend.ps1 -OutputPath /tmp/test-build-script -ProjectRoot .. 2>&1 | head -20',
    { 
      cwd: path.join(__dirname, '..'),
      encoding: 'utf8',
      stdio: 'pipe'
    }
  );
  
  if (result.includes('Project File: ../Aura.Api/Aura.Api.csproj') && 
      result.includes('Project Root: ..')) {
    console.log('✓ Script correctly resolves project file path from Desktop directory\n');
  } else {
    console.error('✗ Script did not show expected path resolution');
    console.error('Output:', result);
    process.exit(1);
  }
} catch (error) {
  // Build may fail due to other issues, but we just need to verify path resolution
  const output = error.stdout || error.stderr || '';
  if (output.includes('Project File: ../Aura.Api/Aura.Api.csproj')) {
    console.log('✓ Script correctly resolves project file path from Desktop directory\n');
  } else {
    console.error('✗ Test failed with unexpected error');
    console.error('Output:', output);
    process.exit(1);
  }
}

// Test 2: Verify error handling with invalid path
console.log('Test 2: Testing error handling with invalid ProjectRoot');
try {
  execSync(
    'pwsh -File ../build-backend.ps1 -OutputPath /tmp/test-bad -ProjectRoot /invalid/path 2>&1',
    {
      cwd: path.join(__dirname, '..'),
      encoding: 'utf8',
      stdio: 'pipe'
    }
  );
  console.error('✗ Script should have failed with invalid path');
  process.exit(1);
} catch (error) {
  const output = error.stdout || error.stderr || '';
  if (output.includes('ERROR: Project file not found') && 
      output.includes('Current directory:') &&
      output.includes('Script root:')) {
    console.log('✓ Script shows clear error messages with diagnostic information\n');
  } else {
    console.error('✗ Error message format incorrect');
    console.error('Output:', output);
    process.exit(1);
  }
}

// Test 3: Verify script works from repository root (default behavior)
console.log('Test 3: Calling script from repository root (default ProjectRoot)');
try {
  const result = execSync(
    'pwsh -File build-backend.ps1 -OutputPath /tmp/test-build-root 2>&1 | head -20',
    {
      cwd: path.join(__dirname, '../..'),
      encoding: 'utf8',
      stdio: 'pipe'
    }
  );
  
  if (result.includes('Aura.Api/Aura.Api.csproj')) {
    console.log('✓ Script works with default ProjectRoot from repository root\n');
  } else {
    console.error('✗ Script did not find project file with default ProjectRoot');
    console.error('Output:', result);
    process.exit(1);
  }
} catch (error) {
  const output = error.stdout || error.stderr || '';
  if (output.includes('Aura.Api/Aura.Api.csproj')) {
    console.log('✓ Script works with default ProjectRoot from repository root\n');
  } else {
    console.error('✗ Test failed with unexpected error');
    console.error('Output:', output);
    process.exit(1);
  }
}

console.log('✅ All path resolution tests passed!');
console.log('\nSummary:');
console.log('- Script correctly resolves paths when called from Desktop directory');
console.log('- Error handling provides clear diagnostic messages');
console.log('- Default ProjectRoot ($PSScriptRoot) works from repository root');
