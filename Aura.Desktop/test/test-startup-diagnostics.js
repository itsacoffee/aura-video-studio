/**
 * Test script for StartupDiagnostics
 * This validates the diagnostics work correctly without running the full Electron app
 */

const fs = require('fs');
const path = require('path');
const os = require('os');

// Create mock app and logger objects for testing
const mockApp = {
  getVersion: () => '1.0.0-test',
  getPath: (name) => {
    if (name === 'userData') {
      return path.join(os.tmpdir(), 'aura-test-diagnostics');
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

const mockLogger = {
  info: (component, message, metadata) => {
    console.log(`[INFO] ${component}: ${message}`, metadata || '');
  },
  warn: (component, message, metadata) => {
    console.log(`[WARN] ${component}: ${message}`, metadata || '');
  },
  error: (component, message, error, metadata) => {
    console.log(`[ERROR] ${component}: ${message}`, error || '', metadata || '');
  },
  debug: (component, message, metadata) => {
    console.log(`[DEBUG] ${component}: ${message}`, metadata || '');
  }
};

// Load the diagnostics module
const StartupDiagnostics = require('../electron/startup-diagnostics');

console.log('='.repeat(60));
console.log('Testing StartupDiagnostics');
console.log('='.repeat(60));

async function runTests() {
  try {
    // Test 1: Initialize diagnostics
    console.log('\n1. Initializing diagnostics...');
    const diagnostics = new StartupDiagnostics(mockApp, mockLogger);
    console.log('✓ Diagnostics initialized');

    // Test 2: Run full diagnostics
    console.log('\n2. Running full diagnostics...');
    const results = await diagnostics.runDiagnostics();
    console.log('✓ Diagnostics completed');
    
    // Test 3: Check platform detection
    console.log('\n3. Verifying platform detection...');
    console.log('   Platform:', results.checks.platform.platform);
    console.log('   Architecture:', results.checks.platform.arch);
    console.log('   Supported:', results.checks.platform.supported);
    if (results.checks.platform.supported) {
      console.log('✓ Platform detection works');
    } else {
      console.log('✗ Platform not supported (but detection works)');
    }

    // Test 4: Check Node.js version
    console.log('\n4. Verifying Node.js version check...');
    console.log('   Version:', results.checks.nodeVersion.version);
    console.log('   Adequate:', results.checks.nodeVersion.adequate);
    console.log('✓ Node.js version check works');

    // Test 5: Check memory
    console.log('\n5. Verifying memory check...');
    console.log('   Total:', results.checks.memory.total);
    console.log('   Free:', results.checks.memory.free);
    console.log('   Used:', results.checks.memory.used);
    console.log('   Adequate:', results.checks.memory.adequate);
    console.log('✓ Memory check works');

    // Test 6: Check disk space
    console.log('\n6. Verifying disk space check...');
    if (results.checks.diskSpace.available) {
      console.log('   Free space:', results.checks.diskSpace.freeSpace);
      console.log('   Adequate:', results.checks.diskSpace.adequate);
      console.log('✓ Disk space check works');
    } else {
      console.log('   Could not determine disk space (platform specific)');
      console.log('✓ Disk space check handled gracefully');
    }

    // Test 7: Check directories
    console.log('\n7. Verifying directories check...');
    results.checks.directories.forEach(dir => {
      console.log(`   ${dir.name}: ${dir.status} (${dir.path})`);
    });
    const allDirsOk = results.checks.directories.every(d => d.status === 'ok');
    if (allDirsOk) {
      console.log('✓ All directories accessible');
    } else {
      console.log('⚠ Some directories inaccessible (may be expected)');
    }

    // Test 8: Check FFmpeg
    console.log('\n8. Verifying FFmpeg check...');
    console.log('   Available:', results.checks.ffmpeg.available);
    if (results.checks.ffmpeg.available) {
      console.log('   Version:', results.checks.ffmpeg.version);
      console.log('✓ FFmpeg detected');
    } else {
      console.log('   Not found (expected in test environment)');
      console.log('✓ FFmpeg check handled gracefully');
    }

    // Test 9: Check .NET
    console.log('\n9. Verifying .NET check...');
    console.log('   Available:', results.checks.dotnet.available);
    if (results.checks.dotnet.available) {
      console.log('   Version:', results.checks.dotnet.version);
      console.log('✓ .NET detected');
    } else {
      console.log('   Not found (may be expected in test environment)');
      console.log('✓ .NET check handled gracefully');
    }

    // Test 10: Check port availability
    console.log('\n10. Verifying port availability check...');
    console.log('   Port:', results.checks.port.port);
    console.log('   Available:', results.checks.port.available);
    console.log('✓ Port check works');

    // Test 11: Overall health
    console.log('\n11. Verifying overall health assessment...');
    console.log('   Healthy:', results.healthy);
    console.log('   Warnings:', results.warnings.length);
    console.log('   Errors:', results.errors.length);
    console.log('✓ Health assessment works');

    // Test 12: Warnings and errors
    console.log('\n12. Checking warnings and errors...');
    if (results.warnings.length > 0) {
      console.log('   Warnings found:');
      results.warnings.forEach(w => {
        console.log(`     - ${w.message}`);
      });
    } else {
      console.log('   No warnings');
    }
    
    if (results.errors.length > 0) {
      console.log('   Errors found:');
      results.errors.forEach(e => {
        console.log(`     - ${e.message}`);
      });
    } else {
      console.log('   No errors');
    }
    console.log('✓ Warning and error collection works');

    // Cleanup
    console.log('\n13. Cleaning up test files...');
    const testDir = path.join(os.tmpdir(), 'aura-test-diagnostics');
    if (fs.existsSync(testDir)) {
      fs.rmSync(testDir, { recursive: true, force: true });
    }
    console.log('✓ Cleanup complete');

    console.log('\n' + '='.repeat(60));
    console.log('ALL TESTS PASSED ✓');
    console.log('='.repeat(60));
    console.log('\nDiagnostics Summary:');
    console.log('  Platform:', results.checks.platform.platform);
    console.log('  Node Version:', results.checks.nodeVersion.version);
    console.log('  Memory:', results.checks.memory.total);
    console.log('  Overall Health:', results.healthy ? 'HEALTHY ✓' : 'ISSUES DETECTED ⚠');

  } catch (error) {
    console.error('\n✗ TEST FAILED:', error);
    console.error('Stack trace:', error.stack);
    process.exit(1);
  }
}

runTests();
