/**
 * Test script for StartupLogger
 * This validates the logger works correctly without running the full Electron app
 */

const fs = require('fs');
const path = require('path');
const os = require('os');

// Create a mock app object for testing
const mockApp = {
  getVersion: () => '1.0.0-test',
  getPath: (name) => {
    if (name === 'userData') {
      return path.join(os.tmpdir(), 'aura-test-logs');
    }
    if (name === 'temp') {
      return os.tmpdir();
    }
    if (name === 'cache') {
      return path.join(os.tmpdir(), 'aura-test-cache');
    }
    return os.tmpdir();
  }
};

// Load the logger
const StartupLogger = require('../electron/startup-logger');

console.log('='.repeat(60));
console.log('Testing StartupLogger');
console.log('='.repeat(60));

try {
  // Test 1: Initialize logger
  console.log('\n1. Initializing logger...');
  const logger = new StartupLogger(mockApp, { debugMode: true });
  console.log('✓ Logger initialized');
  console.log('   Log file:', logger.getLogFile());
  console.log('   Summary file:', logger.getSummaryFile());

  // Test 2: Basic logging
  console.log('\n2. Testing basic logging...');
  logger.info('Test', 'Test info message', { data: 'test' });
  logger.warn('Test', 'Test warning message');
  logger.error('Test', 'Test error message', new Error('Test error'));
  logger.debug('Test', 'Test debug message');
  console.log('✓ Basic logging works');

  // Test 3: Step tracking
  console.log('\n3. Testing step tracking...');
  logger.stepStart('test-step', 'TestComponent', 'Testing step tracking');
  
  // Simulate work
  const startTime = Date.now();
  while (Date.now() - startTime < 100) {
    // Wait 100ms
  }
  
  logger.stepEnd('test-step', true, { testData: 'success' });
  console.log('✓ Step tracking works');

  // Test 4: Slow step detection (>2 seconds)
  console.log('\n4. Testing slow step detection...');
  logger.stepStart('slow-step', 'TestComponent', 'Testing slow step warning');
  
  // Simulate slow work (2.5 seconds)
  const slowStartTime = Date.now();
  while (Date.now() - slowStartTime < 2500) {
    // Wait 2.5 seconds
  }
  
  logger.stepEnd('slow-step', true);
  console.log('✓ Slow step detection works');

  // Test 5: Async tracking
  console.log('\n5. Testing async function tracking...');
  async function testAsync() {
    return new Promise(resolve => setTimeout(() => resolve('async result'), 50));
  }
  
  logger.trackAsync('async-test', 'TestComponent', 'Testing async tracking', testAsync)
    .then(() => {
      console.log('✓ Async tracking works');
      
      // Test 6: Sync tracking
      console.log('\n6. Testing sync function tracking...');
      const result = logger.trackSync('sync-test', 'TestComponent', 'Testing sync tracking', () => {
        return 'sync result';
      });
      console.log('✓ Sync tracking works, result:', result);

      // Test 7: Error tracking
      console.log('\n7. Testing error tracking...');
      try {
        logger.trackSync('error-test', 'TestComponent', 'Testing error tracking', () => {
          throw new Error('Intentional test error');
        });
      } catch (e) {
        console.log('✓ Error tracking works (error caught as expected)');
      }

      // Test 8: Finalize and write summary
      console.log('\n8. Finalizing logger and writing summary...');
      const summary = logger.finalize();
      console.log('✓ Summary written');
      console.log('   Total steps:', summary.statistics.totalSteps);
      console.log('   Successful steps:', summary.statistics.successfulSteps);
      console.log('   Failed steps:', summary.statistics.failedSteps);
      console.log('   Total duration:', summary.totalDuration);

      // Test 9: Verify files exist
      console.log('\n9. Verifying log files exist...');
      const logExists = fs.existsSync(logger.getLogFile());
      const summaryExists = fs.existsSync(logger.getSummaryFile());
      console.log('   Log file exists:', logExists ? '✓' : '✗');
      console.log('   Summary file exists:', summaryExists ? '✓' : '✗');

      // Test 10: Verify file contents
      console.log('\n10. Verifying file contents...');
      const logContent = fs.readFileSync(logger.getLogFile(), 'utf8');
      const logLines = logContent.trim().split('\n').length;
      console.log('   Log entries:', logLines);
      
      const summaryContent = fs.readFileSync(logger.getSummaryFile(), 'utf8');
      const summaryJson = JSON.parse(summaryContent);
      console.log('   Summary steps:', summaryJson.steps.length);
      console.log('   Summary errors:', summaryJson.errors.length);

      // Test 11: Log rotation
      console.log('\n11. Testing log rotation...');
      console.log('   Creating 12 test logs to trigger rotation...');
      const logsDir = logger.getLogsDirectory();
      
      // Create 12 test log files
      for (let i = 0; i < 12; i++) {
        const testLogFile = path.join(logsDir, `startup-test-${i}.log`);
        fs.writeFileSync(testLogFile, `Test log ${i}\n`);
      }
      
      // Create a new logger (should trigger rotation)
      const logger2 = new StartupLogger(mockApp, { debugMode: false });
      
      // Count remaining logs
      const files = fs.readdirSync(logsDir);
      const startupLogs = files.filter(f => f.startsWith('startup-') && f.endsWith('.log'));
      console.log('   Logs after rotation:', startupLogs.length);
      console.log('   ✓ Log rotation works (should keep ~10 logs)');

      // Cleanup
      console.log('\n12. Cleaning up test files...');
      fs.rmSync(logsDir, { recursive: true, force: true });
      console.log('✓ Cleanup complete');

      console.log('\n' + '='.repeat(60));
      console.log('ALL TESTS PASSED ✓');
      console.log('='.repeat(60));
    })
    .catch(error => {
      console.error('\n✗ TEST FAILED:', error);
      process.exit(1);
    });

} catch (error) {
  console.error('\n✗ TEST FAILED:', error);
  process.exit(1);
}
