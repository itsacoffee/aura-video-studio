/**
 * Test script for InitializationTracker
 * Tests initialization step tracking with deliberate error scenarios
 */

const os = require('os');
const path = require('path');
const fs = require('fs');

// Mock app object
const mockApp = {
  getVersion: () => '1.0.0-test',
  getPath: (name) => {
    if (name === 'userData') {
      return path.join(os.tmpdir(), 'aura-init-test-logs');
    }
    return os.tmpdir();
  }
};

// Load modules
const { InitializationTracker, InitializationStep, StepCriticality, InitializationStatus } = require('../electron/initialization-tracker');

console.log('='.repeat(60));
console.log('Testing InitializationTracker');
console.log('='.repeat(60));

let passed = 0;
let failed = 0;

function test(description, fn) {
  try {
    fn();
    console.log(`✓ ${description}`);
    passed++;
  } catch (error) {
    console.error(`✗ ${description}`);
    console.error('  Error:', error.message);
    failed++;
  }
}

function assertEquals(actual, expected, message) {
  if (actual !== expected) {
    throw new Error(`${message}: expected ${expected}, got ${actual}`);
  }
}

function assertTrue(value, message) {
  if (!value) {
    throw new Error(message);
  }
}

function assertFalse(value, message) {
  if (value) {
    throw new Error(message);
  }
}

// Test 1: InitializationStatus creation
console.log('\n1. Testing InitializationStatus creation...');
test('Should create initialization status with correct initial state', () => {
  const status = new InitializationStatus(InitializationStep.APP_CONFIG);
  assertEquals(status.step, InitializationStep.APP_CONFIG, 'Step should match');
  assertEquals(status.status, 'pending', 'Initial status should be pending');
  assertEquals(status.criticality, StepCriticality.CRITICAL, 'AppConfig should be critical');
});

// Test 2: InitializationStatus lifecycle
console.log('\n2. Testing InitializationStatus lifecycle...');
test('Should track step lifecycle correctly', () => {
  const status = new InitializationStatus(InitializationStep.SPLASH_SCREEN);
  
  // Start step
  status.start();
  assertEquals(status.status, 'in-progress', 'Status should be in-progress');
  assertTrue(status.startTime !== null, 'Start time should be set');
  
  // Wait a bit
  const startTime = Date.now();
  while (Date.now() - startTime < 50) {} // 50ms delay
  
  // Succeed step
  status.succeed({ test: 'data' });
  assertEquals(status.status, 'success', 'Status should be success');
  assertTrue(status.endTime !== null, 'End time should be set');
  assertTrue(status.duration > 0, 'Duration should be positive');
  assertTrue(status.isSuccessful(), 'Should report as successful');
});

// Test 3: InitializationStatus failure handling
console.log('\n3. Testing InitializationStatus failure handling...');
test('Should track failures with error details', () => {
  const status = new InitializationStatus(InitializationStep.BACKEND_SERVICE);
  status.start();
  
  const error = new Error('Backend failed to start');
  error.code = 'ECONNREFUSED';
  
  status.fail(error, 'Check if .NET runtime is installed');
  
  assertEquals(status.status, 'failed', 'Status should be failed');
  assertEquals(status.errorMessage, 'Backend failed to start', 'Error message should match');
  assertEquals(status.recoveryAction, 'Check if .NET runtime is installed', 'Recovery action should match');
  assertTrue(status.isCriticalFailure(), 'Backend failure should be critical');
});

// Test 4: InitializationTracker creation
console.log('\n4. Testing InitializationTracker creation...');
test('Should initialize tracker with all steps', () => {
  const tracker = new InitializationTracker(mockApp);
  const stepCount = Object.keys(InitializationStep).length;
  assertEquals(tracker.steps.size, stepCount, `Should have ${stepCount} steps`);
});

// Test 5: Step tracking
console.log('\n5. Testing step tracking...');
test('Should start and succeed steps correctly', () => {
  const tracker = new InitializationTracker(mockApp);
  
  tracker.startStep(InitializationStep.STARTUP_LOGGER);
  const status = tracker.getStepStatus(InitializationStep.STARTUP_LOGGER);
  assertEquals(status.status, 'in-progress', 'Step should be in progress');
  
  tracker.succeedStep(InitializationStep.STARTUP_LOGGER, { logFile: '/tmp/test.log' });
  assertEquals(status.status, 'success', 'Step should be successful');
  assertEquals(status.metadata.logFile, '/tmp/test.log', 'Metadata should be stored');
});

// Test 6: Critical failure detection
console.log('\n6. Testing critical failure detection...');
test('Should detect critical failures correctly', () => {
  const tracker = new InitializationTracker(mockApp);
  
  // Succeed some critical steps
  tracker.startStep(InitializationStep.APP_CONFIG);
  tracker.succeedStep(InitializationStep.APP_CONFIG);
  
  tracker.startStep(InitializationStep.WINDOW_MANAGER);
  tracker.succeedStep(InitializationStep.WINDOW_MANAGER);
  
  // Fail a critical step
  tracker.startStep(InitializationStep.BACKEND_SERVICE);
  tracker.failStep(InitializationStep.BACKEND_SERVICE, new Error('Backend crashed'));
  
  assertFalse(tracker.allCriticalStepsSucceeded(), 'Should report critical failure');
  
  const failures = tracker.getCriticalFailures();
  assertEquals(failures.length, 1, 'Should have one critical failure');
  assertEquals(failures[0].step, InitializationStep.BACKEND_SERVICE, 'Failed step should be backend');
});

// Test 7: Optional step skipping
console.log('\n7. Testing optional step skipping...');
test('Should allow skipping optional steps without affecting critical status', () => {
  const tracker = new InitializationTracker(mockApp);
  
  // Succeed all critical steps
  tracker.startStep(InitializationStep.ERROR_HANDLING);
  tracker.succeedStep(InitializationStep.ERROR_HANDLING);
  
  tracker.startStep(InitializationStep.APP_CONFIG);
  tracker.succeedStep(InitializationStep.APP_CONFIG);
  
  tracker.startStep(InitializationStep.WINDOW_MANAGER);
  tracker.succeedStep(InitializationStep.WINDOW_MANAGER);
  
  tracker.startStep(InitializationStep.BACKEND_SERVICE);
  tracker.succeedStep(InitializationStep.BACKEND_SERVICE);
  
  tracker.startStep(InitializationStep.IPC_HANDLERS);
  tracker.succeedStep(InitializationStep.IPC_HANDLERS);
  
  tracker.startStep(InitializationStep.MAIN_WINDOW);
  tracker.succeedStep(InitializationStep.MAIN_WINDOW);
  
  // Skip optional steps
  tracker.skipStep(InitializationStep.SPLASH_SCREEN, 'Window creation failed');
  tracker.skipStep(InitializationStep.SYSTEM_TRAY, 'Icon not found');
  tracker.skipStep(InitializationStep.AUTO_UPDATER, 'Disabled in dev mode');
  
  assertTrue(tracker.allCriticalStepsSucceeded(), 'All critical steps should be successful');
});

// Test 8: Completion percentage
console.log('\n8. Testing completion percentage calculation...');
test('Should calculate completion percentage correctly', () => {
  const tracker = new InitializationTracker(mockApp);
  
  assertEquals(tracker.getCompletionPercentage(), 0, 'Should start at 0%');
  
  // Complete half the steps
  const steps = Array.from(tracker.steps.keys());
  const halfSteps = Math.floor(steps.length / 2);
  
  for (let i = 0; i < halfSteps; i++) {
    tracker.startStep(steps[i]);
    tracker.succeedStep(steps[i]);
  }
  
  const percentage = tracker.getCompletionPercentage();
  assertTrue(percentage >= 45 && percentage <= 55, `Completion should be around 50%, got ${percentage}%`);
});

// Test 9: Summary generation
console.log('\n9. Testing summary generation...');
test('Should generate comprehensive summary', () => {
  const tracker = new InitializationTracker(mockApp);
  
  // Simulate mixed results
  tracker.startStep(InitializationStep.STARTUP_LOGGER);
  tracker.succeedStep(InitializationStep.STARTUP_LOGGER);
  
  tracker.startStep(InitializationStep.APP_CONFIG);
  tracker.failStep(InitializationStep.APP_CONFIG, new Error('Config corrupted'), 'Use defaults');
  
  tracker.skipStep(InitializationStep.SYSTEM_TRAY, 'Not available');
  
  const summary = tracker.getSummary();
  
  assertTrue(summary.totalSteps > 0, 'Should have total steps');
  assertEquals(summary.successCount, 1, 'Should have 1 success');
  assertEquals(summary.failureCount, 1, 'Should have 1 failure');
  assertTrue(summary.steps.length > 0, 'Should have step details');
  assertFalse(summary.allCriticalStepsSucceeded, 'Critical steps should not all succeed');
});

// Test 10: Summary file writing
console.log('\n10. Testing summary file writing...');
test('Should write summary to disk', () => {
  const tracker = new InitializationTracker(mockApp);
  
  tracker.startStep(InitializationStep.STARTUP_LOGGER);
  tracker.succeedStep(InitializationStep.STARTUP_LOGGER);
  
  const summaryFile = tracker.writeSummary();
  
  assertTrue(summaryFile !== null, 'Should return summary file path');
  assertTrue(fs.existsSync(summaryFile), 'Summary file should exist');
  
  const content = fs.readFileSync(summaryFile, 'utf8');
  const summary = JSON.parse(content);
  
  assertEquals(summary.successCount, 1, 'Summary should have correct success count');
});

// Test 11: Deliberate error scenario - All failures
console.log('\n11. Testing deliberate error scenario - All failures...');
test('Should handle scenario where all steps fail', () => {
  const tracker = new InitializationTracker(mockApp);
  
  const steps = Array.from(tracker.steps.keys());
  steps.forEach(step => {
    tracker.startStep(step);
    tracker.failStep(step, new Error(`${step} failed`), 'Restart application');
  });
  
  assertFalse(tracker.allCriticalStepsSucceeded(), 'No critical steps should succeed');
  
  const criticalFailures = tracker.getCriticalFailures();
  assertTrue(criticalFailures.length > 0, 'Should have critical failures');
  
  const summary = tracker.getSummary();
  assertEquals(summary.successCount, 0, 'No steps should succeed');
  assertEquals(summary.failureCount, steps.length, 'All steps should fail');
});

// Test 12: Deliberate error scenario - Partial failures
console.log('\n12. Testing deliberate error scenario - Partial failures...');
test('Should handle partial failures with degraded mode', () => {
  const tracker = new InitializationTracker(mockApp);
  
  // Critical steps succeed
  tracker.startStep(InitializationStep.ERROR_HANDLING);
  tracker.succeedStep(InitializationStep.ERROR_HANDLING);
  
  tracker.startStep(InitializationStep.APP_CONFIG);
  tracker.succeedStep(InitializationStep.APP_CONFIG);
  
  tracker.startStep(InitializationStep.WINDOW_MANAGER);
  tracker.succeedStep(InitializationStep.WINDOW_MANAGER);
  
  tracker.startStep(InitializationStep.BACKEND_SERVICE);
  tracker.succeedStep(InitializationStep.BACKEND_SERVICE);
  
  tracker.startStep(InitializationStep.IPC_HANDLERS);
  tracker.succeedStep(InitializationStep.IPC_HANDLERS);
  
  tracker.startStep(InitializationStep.MAIN_WINDOW);
  tracker.succeedStep(InitializationStep.MAIN_WINDOW);
  
  // Important/optional steps fail
  tracker.startStep(InitializationStep.PROTOCOL_HANDLER);
  tracker.failStep(InitializationStep.PROTOCOL_HANDLER, new Error('Protocol registration failed'));
  
  tracker.startStep(InitializationStep.APP_MENU);
  tracker.failStep(InitializationStep.APP_MENU, new Error('Menu creation failed'));
  
  tracker.skipStep(InitializationStep.AUTO_UPDATER, 'Disabled');
  
  assertTrue(tracker.allCriticalStepsSucceeded(), 'Critical steps should succeed');
  
  const failedSteps = tracker.getFailedSteps();
  assertEquals(failedSteps.length, 2, 'Should have 2 failed steps');
  
  const criticalFailures = tracker.getCriticalFailures();
  assertEquals(criticalFailures.length, 0, 'Should have no critical failures');
});

// Cleanup
console.log('\n13. Cleaning up test files...');
const logsDir = path.join(os.tmpdir(), 'aura-init-test-logs');
if (fs.existsSync(logsDir)) {
  fs.rmSync(logsDir, { recursive: true, force: true });
  console.log('✓ Cleanup complete');
}

// Summary
console.log('\n' + '='.repeat(60));
console.log('TEST RESULTS');
console.log('='.repeat(60));
console.log(`Passed: ${passed}`);
console.log(`Failed: ${failed}`);
console.log(`Total: ${passed + failed}`);
console.log('='.repeat(60));

if (failed > 0) {
  process.exit(1);
} else {
  console.log('\nALL TESTS PASSED ✓');
}
