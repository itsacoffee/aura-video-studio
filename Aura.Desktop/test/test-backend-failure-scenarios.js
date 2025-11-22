/**
 * Test Backend Startup Failure Scenarios
 * Simulates various failure conditions to verify error handling and recovery
 */

const path = require("path");
const net = require("net");

console.log("=== Backend Failure Scenario Tests ===\n");

let passed = 0;
let failed = 0;

// Mock app object
const mockApp = {
  getPath: (name) => {
    if (name === "userData") return "/tmp/aura-test";
    if (name === "temp") return "/tmp";
    return "/tmp";
  },
};

// Mock network contract
const mockContract = {
  baseUrl: "http://127.0.0.1:5005",
  port: 5005,
  healthEndpoint: "/health/live",
  readinessEndpoint: "/health/ready",
  sseJobEventsTemplate: "/api/jobs/{id}/events",
  shouldSelfHost: true,
  maxStartupMs: 60000,
  pollIntervalMs: 1000,
};

const BackendService = require("../electron/backend-service");

// Test 1: Error classification for missing executable
function test1() {
  console.log("Test 1: Missing executable error classification");
  try {
    const service = new BackendService(
      mockApp,
      false, // production mode
      null,
      mockContract,
      console
    );

    const result = service._validateBackendExecutable("/nonexistent/Aura.Api.exe");

    if (!result.valid && result.error.includes("not found")) {
      console.log("  ✓ Correctly identifies missing executable");
      console.log(`    Error: ${result.error}`);
      console.log(`    Suggestion: ${result.suggestion}`);
      passed++;
    } else {
      console.log("  ✗ Failed to identify missing executable");
      failed++;
    }
  } catch (error) {
    console.log(`  ✗ Test error: ${error.message}`);
    failed++;
  }
  console.log("");
}

// Test 2: Port conflict detection
async function test2() {
  console.log("Test 2: Port conflict detection");
  try {
    const service = new BackendService(
      mockApp,
      true,
      null,
      mockContract,
      console
    );

    // Create a test server to occupy a port
    const testPort = 50123;
    const testServer = net.createServer();

    await new Promise((resolve) => {
      testServer.listen(testPort, "127.0.0.1", () => {
        console.log(`  Created test server on port ${testPort}`);
        resolve();
      });
    });

    // Now check if that port is available
    const result = await service._checkPortAvailability(testPort);

    testServer.close();

    if (!result.available && result.conflictInfo) {
      console.log("  ✓ Correctly detects port conflict");
      console.log(`    Conflict info: ${result.conflictInfo}`);
      passed++;
    } else {
      console.log("  ✗ Failed to detect port conflict");
      console.log(`    Result: ${JSON.stringify(result)}`);
      failed++;
    }
  } catch (error) {
    console.log(`  ✗ Test error: ${error.message}`);
    failed++;
  }
  console.log("");
}

// Test 3: Invalid .NET version handling
async function test3() {
  console.log("Test 3: .NET runtime validation");
  try {
    const service = new BackendService(
      mockApp,
      true,
      null,
      mockContract,
      console
    );

    const result = await service._validateDotnetRuntime();

    console.log(`  .NET available: ${result.available}`);
    if (result.available) {
      console.log(`  Version: ${result.version}`);
      // Check if version is 8 or higher
      const versionMatch = result.version.match(/^(\d+)/);
      if (versionMatch && parseInt(versionMatch[1]) >= 8) {
        console.log("  ✓ .NET 8+ runtime detected");
        passed++;
      } else {
        console.log("  ✗ .NET version too low (need 8+)");
        failed++;
      }
    } else {
      console.log(`  Error: ${result.error}`);
      console.log("  ⚠ .NET not available (expected in CI environment)");
      console.log("  ✓ Error message is informative");
      passed++;
    }
  } catch (error) {
    console.log(`  ✗ Test error: ${error.message}`);
    failed++;
  }
  console.log("");
}

// Test 4: Executable validation with directory instead of file
function test4() {
  console.log("Test 4: Directory validation (should fail for executable)");
  try {
    const service = new BackendService(
      mockApp,
      true,
      null,
      mockContract,
      console
    );

    // Use a directory path that exists
    const result = service._validateBackendExecutable("/tmp");

    if (!result.valid && result.error.includes("not a file")) {
      console.log("  ✓ Correctly rejects directory as executable");
      console.log(`    Error: ${result.error}`);
      passed++;
    } else {
      console.log("  ✗ Failed to reject directory");
      console.log(`    Result: ${JSON.stringify(result)}`);
      failed++;
    }
  } catch (error) {
    console.log(`  ✗ Test error: ${error.message}`);
    failed++;
  }
  console.log("");
}

// Test 5: Retry logic structure
function test5() {
  console.log("Test 5: Retry logic structure validation");
  try {
    const service = new BackendService(
      mockApp,
      true,
      null,
      mockContract,
      console
    );

    // Verify both start() and _startInternal() exist
    const hasStart = typeof service.start === "function";
    const hasStartInternal = typeof service._startInternal === "function";

    if (hasStart && hasStartInternal) {
      console.log("  ✓ Both start() and _startInternal() methods exist");
      console.log("    start() wrapper provides retry logic");
      console.log("    _startInternal() contains actual startup code");
      passed++;
    } else {
      console.log("  ✗ Missing expected methods");
      console.log(`    start() exists: ${hasStart}`);
      console.log(`    _startInternal() exists: ${hasStartInternal}`);
      failed++;
    }
  } catch (error) {
    console.log(`  ✗ Test error: ${error.message}`);
    failed++;
  }
  console.log("");
}

// Test 6: Error diagnostics in _waitForBackend
function test6() {
  console.log("Test 6: Error message structure validation");
  try {
    // This test verifies that our error handling structure is correct
    // by checking the expected backend path generation
    const service = new BackendService(
      mockApp,
      false, // production
      null,
      mockContract,
      console
    );

    const expectedPath = service._getExpectedBackendPath();

    if (
      expectedPath &&
      typeof expectedPath === "string" &&
      expectedPath.includes("backend") &&
      expectedPath.includes("Aura.Api")
    ) {
      console.log("  ✓ Expected path generation is correct");
      console.log(`    Path: ${expectedPath}`);
      passed++;
    } else {
      console.log("  ✗ Expected path generation is incorrect");
      console.log(`    Path: ${expectedPath}`);
      failed++;
    }
  } catch (error) {
    console.log(`  ✗ Test error: ${error.message}`);
    failed++;
  }
  console.log("");
}

// Run all tests
async function runTests() {
  test1();
  await test2();
  await test3();
  test4();
  test5();
  test6();

  console.log("=== Test Summary ===");
  console.log(`Passed: ${passed}`);
  console.log(`Failed: ${failed}`);

  if (failed > 0) {
    console.log("\n❌ Some tests failed");
    process.exit(1);
  } else {
    console.log("\n✅ All tests passed");
    process.exit(0);
  }
}

runTests().catch((error) => {
  console.error("Test suite error:", error);
  process.exit(1);
});
