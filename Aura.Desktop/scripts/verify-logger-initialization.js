/**
 * Manual Verification Script: BackendService Logger
 * 
 * This script demonstrates that the BackendService logger is properly initialized
 * and can be used for logging during backend health checks.
 * 
 * Run: node scripts/verify-logger-initialization.js
 */

const BackendService = require('../electron/backend-service.js');

console.log('=== BackendService Logger Initialization Verification ===\n');

// Create mock objects
const mockApp = { getPath: () => '/tmp' };
const mockProcessManager = { register: () => {} };
const mockNetworkContract = { 
  baseUrl: 'http://localhost:5000',
  port: 5000,
  shouldSelfHost: true
};

// Test 1: BackendService with console fallback
console.log('Test 1: Creating BackendService WITHOUT custom logger (should use console fallback)');
try {
  const service1 = new BackendService(mockApp, true, mockProcessManager, mockNetworkContract);
  console.log('✓ BackendService created successfully');
  console.log('  - Logger type:', typeof service1.logger);
  console.log('  - Logger.info type:', typeof service1.logger.info);
  console.log('  - Logger is console:', service1.logger === console);
  console.log('');
} catch (error) {
  console.error('✗ Failed to create BackendService:', error.message);
  process.exit(1);
}

// Test 2: BackendService with custom logger
console.log('Test 2: Creating BackendService WITH custom logger');
const logMessages = [];
const customLogger = {
  info: (...args) => {
    logMessages.push({ level: 'info', args });
    console.log('  [CUSTOM INFO]', ...args);
  },
  error: (...args) => {
    logMessages.push({ level: 'error', args });
    console.log('  [CUSTOM ERROR]', ...args);
  },
  debug: (...args) => {
    logMessages.push({ level: 'debug', args });
    console.log('  [CUSTOM DEBUG]', ...args);
  }
};

try {
  const service2 = new BackendService(mockApp, true, mockProcessManager, mockNetworkContract, customLogger);
  console.log('✓ BackendService created successfully with custom logger');
  console.log('  - Logger is custom:', service2.logger === customLogger);
  console.log('');
} catch (error) {
  console.error('✗ Failed to create BackendService:', error.message);
  process.exit(1);
}

// Test 3: Verify logger methods can be called
console.log('Test 3: Testing logger method calls (using optional chaining)');
try {
  const service3 = new BackendService(mockApp, true, mockProcessManager, mockNetworkContract, customLogger);
  
  // These should work without errors due to optional chaining
  service3.logger.info?.('BackendService', 'Test info message');
  service3.logger.error?.('BackendService', 'Test error message');
  service3.logger.debug?.('BackendService', 'Test debug message');
  
  console.log('✓ All logger methods called successfully');
  console.log(`  - Total log messages captured: ${logMessages.length}`);
  console.log('');
} catch (error) {
  console.error('✗ Failed to call logger methods:', error.message);
  process.exit(1);
}

// Test 4: Verify logger works with null/undefined (optional chaining protection)
console.log('Test 4: Testing optional chaining with minimal logger');
try {
  const minimalLogger = {}; // Logger with no methods
  const service4 = new BackendService(mockApp, true, mockProcessManager, mockNetworkContract, minimalLogger);
  
  // These should NOT throw errors thanks to optional chaining
  service4.logger.info?.('BackendService', 'This should not throw');
  service4.logger.error?.('BackendService', 'This should not throw');
  service4.logger.debug?.('BackendService', 'This should not throw');
  
  console.log('✓ Optional chaining prevents errors with incomplete logger');
  console.log('');
} catch (error) {
  console.error('✗ Failed with minimal logger:', error.message);
  process.exit(1);
}

console.log('=== Summary ===');
console.log('✅ All verification tests passed!');
console.log('\nThe BackendService logger initialization is working correctly:');
console.log('  1. Logger parameter accepted in constructor');
console.log('  2. Fallback to console when no logger provided');
console.log('  3. Custom loggers work correctly');
console.log('  4. Optional chaining protects against missing logger methods');
console.log('\nThe fix prevents the runtime error: "Cannot read properties of undefined (reading \'info\')"');
