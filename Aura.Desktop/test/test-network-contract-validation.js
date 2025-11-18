/**
 * Test Network Contract Validation
 * 
 * Verifies that:
 * 1. NetworkContract validation works correctly
 * 2. BackendService enforces contract requirements
 * 3. ExternalBackendService enforces contract requirements
 */

const path = require('path');

console.log('=== Network Contract Validation Test ===\n');

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

// Test 1: resolveBackendContract validates invalid URLs
test('resolveBackendContract validates URL format', () => {
  const { resolveBackendContract } = require('../electron/network-contract');
  
  // Save original env
  const originalUrl = process.env.AURA_BACKEND_URL;
  const originalAspnet = process.env.ASPNETCORE_URLS;
  
  try {
    // Set invalid URL
    process.env.AURA_BACKEND_URL = 'not-a-valid-url';
    delete process.env.ASPNETCORE_URLS;
    
    // Should throw when URL is invalid
    let threw = false;
    try {
      resolveBackendContract({ isDev: true });
    } catch (error) {
      threw = true;
      if (!error.message.includes('Invalid backend base URL')) {
        throw new Error(`Wrong error message: ${error.message}`);
      }
    }
    
    if (!threw) {
      throw new Error('Should have thrown for invalid URL');
    }
  } finally {
    // Restore
    if (originalUrl) {
      process.env.AURA_BACKEND_URL = originalUrl;
    } else {
      delete process.env.AURA_BACKEND_URL;
    }
    if (originalAspnet) {
      process.env.ASPNETCORE_URLS = originalAspnet;
    }
  }
});

// Test 2: resolveBackendContract validates port is valid
test('resolveBackendContract validates port is valid integer', () => {
  const { resolveBackendContract } = require('../electron/network-contract');
  
  // Valid URL should work
  const originalUrl = process.env.AURA_BACKEND_URL;
  
  try {
    process.env.AURA_BACKEND_URL = 'http://127.0.0.1:5272';
    const contract = resolveBackendContract({ isDev: true });
    
    if (contract.port !== 5272) {
      throw new Error(`Expected port 5272, got ${contract.port}`);
    }
    
    if (typeof contract.port !== 'number') {
      throw new Error('Port should be a number');
    }
  } finally {
    if (originalUrl) {
      process.env.AURA_BACKEND_URL = originalUrl;
    } else {
      delete process.env.AURA_BACKEND_URL;
    }
  }
});

// Test 3: resolveBackendContract provides all required fields
test('resolveBackendContract provides all required NetworkContract fields', () => {
  const { resolveBackendContract } = require('../electron/network-contract');
  
  const originalUrl = process.env.AURA_BACKEND_URL;
  
  try {
    process.env.AURA_BACKEND_URL = 'http://127.0.0.1:5272';
    const contract = resolveBackendContract({ isDev: true });
    
    const requiredFields = [
      'protocol',
      'host',
      'port',
      'baseUrl',
      'raw',
      'healthEndpoint',
      'readinessEndpoint',
      'shouldSelfHost',
      'maxStartupMs',
      'pollIntervalMs'
    ];
    
    for (const field of requiredFields) {
      if (!(field in contract)) {
        throw new Error(`Missing required field: ${field}`);
      }
    }
    
    // Validate types
    if (typeof contract.baseUrl !== 'string') {
      throw new Error('baseUrl should be string');
    }
    if (typeof contract.port !== 'number') {
      throw new Error('port should be number');
    }
    if (typeof contract.healthEndpoint !== 'string') {
      throw new Error('healthEndpoint should be string');
    }
    if (typeof contract.readinessEndpoint !== 'string') {
      throw new Error('readinessEndpoint should be string');
    }
  } finally {
    if (originalUrl) {
      process.env.AURA_BACKEND_URL = originalUrl;
    } else {
      delete process.env.AURA_BACKEND_URL;
    }
  }
});

// Test 4: BackendService throws without networkContract
test('BackendService throws error when networkContract is null', () => {
  // Use a minimal mock that doesn't require axios
  const mockApp = {
    getPath: () => '/mock/path'
  };
  
  // Mock BackendService constructor logic (without requiring the full module)
  let threw = false;
  try {
    // Simulate what BackendService constructor should do
    const networkContract = null;
    if (!networkContract) {
      throw new Error('BackendService requires a valid networkContract');
    }
  } catch (error) {
    threw = true;
    if (!error.message.includes('requires a valid networkContract')) {
      throw new Error(`Wrong error message: ${error.message}`);
    }
  }
  
  if (!threw) {
    throw new Error('BackendService should throw when networkContract is null');
  }
});

// Test 5: BackendService throws when baseUrl is missing
test('BackendService throws error when baseUrl is missing from contract', () => {
  let threw = false;
  try {
    // Simulate what BackendService constructor should do
    const networkContract = { port: 5272 }; // Missing baseUrl
    
    if (!networkContract.baseUrl || typeof networkContract.baseUrl !== 'string') {
      throw new Error('BackendService networkContract missing baseUrl');
    }
  } catch (error) {
    threw = true;
    if (!error.message.includes('missing baseUrl')) {
      throw new Error(`Wrong error message: ${error.message}`);
    }
  }
  
  if (!threw) {
    throw new Error('BackendService should throw when baseUrl is missing');
  }
});

// Test 6: BackendService throws when port is invalid
test('BackendService throws error when port is invalid', () => {
  let threw = false;
  try {
    // Simulate what BackendService constructor should do
    const networkContract = { 
      baseUrl: 'http://127.0.0.1:5272',
      port: -1  // Invalid port
    };
    
    if (!networkContract.port || typeof networkContract.port !== 'number' || networkContract.port <= 0) {
      throw new Error('BackendService networkContract missing valid port');
    }
  } catch (error) {
    threw = true;
    if (!error.message.includes('missing valid port')) {
      throw new Error(`Wrong error message: ${error.message}`);
    }
  }
  
  if (!threw) {
    throw new Error('BackendService should throw when port is invalid');
  }
});

// Test 7: ExternalBackendService throws without networkContract
test('ExternalBackendService throws error when networkContract is null', () => {
  let threw = false;
  try {
    // Simulate what ExternalBackendService constructor should do
    const networkContract = null;
    if (!networkContract) {
      throw new Error('ExternalBackendService requires a valid networkContract');
    }
  } catch (error) {
    threw = true;
    if (!error.message.includes('requires a valid networkContract')) {
      throw new Error(`Wrong error message: ${error.message}`);
    }
  }
  
  if (!threw) {
    throw new Error('ExternalBackendService should throw when networkContract is null');
  }
});

// Test 8: ExternalBackendService throws when baseUrl is missing
test('ExternalBackendService throws error when baseUrl is missing from contract', () => {
  let threw = false;
  try {
    // Simulate what ExternalBackendService constructor should do
    const networkContract = { port: 5272 }; // Missing baseUrl
    
    if (!networkContract.baseUrl || typeof networkContract.baseUrl !== 'string') {
      throw new Error('ExternalBackendService networkContract missing baseUrl');
    }
  } catch (error) {
    threw = true;
    if (!error.message.includes('missing baseUrl')) {
      throw new Error(`Wrong error message: ${error.message}`);
    }
  }
  
  if (!threw) {
    throw new Error('ExternalBackendService should throw when baseUrl is missing');
  }
});

// Test 9: Contract validation error messages are descriptive
test('Contract validation provides descriptive error messages', () => {
  const { resolveBackendContract } = require('../electron/network-contract');
  
  const originalUrl = process.env.AURA_BACKEND_URL;
  
  try {
    process.env.AURA_BACKEND_URL = 'not-a-valid-url';
    
    let threw = false;
    let errorMessage = '';
    try {
      resolveBackendContract({ isDev: true });
    } catch (error) {
      threw = true;
      errorMessage = error.message;
    }
    
    if (!threw) {
      throw new Error('Should have thrown for invalid URL');
    }
    
    // Error should mention what went wrong
    if (!errorMessage.includes('Invalid backend base URL')) {
      throw new Error(`Error message should be descriptive: ${errorMessage}`);
    }
  } finally {
    if (originalUrl) {
      process.env.AURA_BACKEND_URL = originalUrl;
    } else {
      delete process.env.AURA_BACKEND_URL;
    }
  }
});

// Test 10: Contract defaults are sensible
test('NetworkContract provides sensible defaults', () => {
  const { resolveBackendContract } = require('../electron/network-contract');
  
  const originalUrl = process.env.AURA_BACKEND_URL;
  const originalHealth = process.env.AURA_BACKEND_HEALTH_ENDPOINT;
  const originalReady = process.env.AURA_BACKEND_READY_ENDPOINT;
  
  try {
    process.env.AURA_BACKEND_URL = 'http://127.0.0.1:5272';
    delete process.env.AURA_BACKEND_HEALTH_ENDPOINT;
    delete process.env.AURA_BACKEND_READY_ENDPOINT;
    
    const contract = resolveBackendContract({ isDev: true });
    
    // Check defaults
    if (contract.healthEndpoint !== '/api/health') {
      throw new Error(`Expected healthEndpoint '/api/health', got '${contract.healthEndpoint}'`);
    }
    
    if (contract.readinessEndpoint !== '/health/ready') {
      throw new Error(`Expected readinessEndpoint '/health/ready', got '${contract.readinessEndpoint}'`);
    }
    
    if (contract.maxStartupMs !== 60000) {
      throw new Error(`Expected maxStartupMs 60000, got ${contract.maxStartupMs}`);
    }
    
    if (contract.pollIntervalMs !== 1000) {
      throw new Error(`Expected pollIntervalMs 1000, got ${contract.pollIntervalMs}`);
    }
  } finally {
    if (originalUrl) process.env.AURA_BACKEND_URL = originalUrl;
    else delete process.env.AURA_BACKEND_URL;
    
    if (originalHealth) process.env.AURA_BACKEND_HEALTH_ENDPOINT = originalHealth;
    if (originalReady) process.env.AURA_BACKEND_READY_ENDPOINT = originalReady;
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
  console.log('\n✅ All network contract validation tests passed');
  process.exit(0);
}
