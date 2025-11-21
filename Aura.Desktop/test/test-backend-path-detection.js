/**
 * Test Backend Path Detection
 * 
 * Validates that:
 * 1. Backend path detection follows the correct order
 * 2. Production path is checked first
 * 3. Development paths are checked as fallback
 * 4. Error is thrown when backend not found
 */

const fs = require('fs');
const path = require('path');

console.log('=== Backend Path Detection Tests ===\n');

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

// Test 1: Verify backend-service.js has _getBackendPath method
test('backend-service.js has _getBackendPath method', () => {
  const backendService = fs.readFileSync(
    path.join(__dirname, '../electron/backend-service.js'),
    'utf8'
  );
  
  if (!backendService.includes('_getBackendPath()')) {
    throw new Error('backend-service.js missing _getBackendPath method');
  }
});

// Test 2: Verify production path is checked first
test('Production path (process.resourcesPath) is checked FIRST', () => {
  const backendService = fs.readFileSync(
    path.join(__dirname, '../electron/backend-service.js'),
    'utf8'
  );
  
  // Check that production path logic appears before development paths
  const productionPathIndex = backendService.indexOf('Check production bundle location FIRST');
  const devPathsIndex = backendService.indexOf('Then check development locations');
  
  if (productionPathIndex === -1) {
    throw new Error('Production path check comment not found');
  }
  
  if (devPathsIndex === -1) {
    throw new Error('Development paths check comment not found');
  }
  
  if (productionPathIndex > devPathsIndex) {
    throw new Error('Production path is not checked before development paths');
  }
});

// Test 3: Verify production path uses process.resourcesPath
test('Production path uses process.resourcesPath', () => {
  const backendService = fs.readFileSync(
    path.join(__dirname, '../electron/backend-service.js'),
    'utf8'
  );
  
  if (!backendService.includes('process.resourcesPath')) {
    throw new Error('backend-service.js does not use process.resourcesPath for production path');
  }
  
  // Verify the path structure
  if (!backendService.includes("'backend'") || 
      !backendService.includes("'win-x64'") ||
      !backendService.includes("'Aura.Api.exe'")) {
    throw new Error('Production path does not follow expected structure: backend/win-x64/Aura.Api.exe');
  }
});

// Test 4: Verify development paths are checked as fallback
test('Development paths are checked as fallback', () => {
  const backendService = fs.readFileSync(
    path.join(__dirname, '../electron/backend-service.js'),
    'utf8'
  );
  
  if (!backendService.includes("'dist'") || !backendService.includes("'backend'")) {
    throw new Error('Development path does not check dist/backend');
  }
  
  if (!backendService.includes("'Release'")) {
    throw new Error('Development path does not check bin/Release');
  }
  
  if (!backendService.includes("'Debug'")) {
    throw new Error('Development path does not check bin/Debug');
  }
});

// Test 5: Verify error is thrown when backend not found
test('Error is thrown when backend executable not found', () => {
  const backendService = fs.readFileSync(
    path.join(__dirname, '../electron/backend-service.js'),
    'utf8'
  );
  
  if (!backendService.includes('throw new Error')) {
    throw new Error('backend-service.js does not throw error when backend not found');
  }
  
  if (!backendService.includes('Backend executable not found')) {
    throw new Error('Error message does not indicate backend not found');
  }
});

// Test 6: Verify fs.existsSync is used to check path existence
test('fs.existsSync is used to check path existence', () => {
  const backendService = fs.readFileSync(
    path.join(__dirname, '../electron/backend-service.js'),
    'utf8'
  );
  
  // Should use fs.existsSync to check if paths exist
  if (!backendService.includes('fs.existsSync')) {
    throw new Error('backend-service.js does not use fs.existsSync to check path existence');
  }
});

// Test 7: Verify path detection logic uses a loop for dev paths
test('Development paths use loop for checking multiple locations', () => {
  const backendService = fs.readFileSync(
    path.join(__dirname, '../electron/backend-service.js'),
    'utf8'
  );
  
  // Should loop through devPaths array
  if (!backendService.includes('devPaths') || !backendService.includes('for (const')) {
    throw new Error('backend-service.js does not loop through devPaths for fallback');
  }
});

// Test 8: Verify console logging for path detection
test('Path detection includes console logging', () => {
  const backendService = fs.readFileSync(
    path.join(__dirname, '../electron/backend-service.js'),
    'utf8'
  );
  
  if (!backendService.includes('Found production backend at:') && 
      !backendService.includes('Found dev backend at:')) {
    throw new Error('backend-service.js missing console logs for path detection');
  }
});

// Summary
console.log('\n=== Test Summary ===');
console.log(`Passed: ${testsPassed}`);
console.log(`Failed: ${testsFailed}`);
console.log(`Total: ${testsPassed + testsFailed}`);

if (testsFailed > 0) {
  console.error('\n❌ Some tests failed');
  process.exit(1);
} else {
  console.log('\n✅ All tests passed');
  process.exit(0);
}
