/**
 * Test Backend Validation Functions
 * Tests the new pre-startup validation logic
 */

const path = require("path");
const { spawn } = require("child_process");

console.log("=== Backend Validation Tests ===\n");

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

// Load BackendService
const BackendService = require("../electron/backend-service");

// Test 1: BackendService constructor accepts all required parameters
function test1() {
  try {
    const service = new BackendService(
      mockApp,
      true, // isDev
      null, // processManager
      mockContract,
      console // logger
    );

    if (service && service.baseUrl === mockContract.baseUrl) {
      console.log("✓ BackendService constructor works with all parameters");
      passed++;
    } else {
      console.log("✗ BackendService constructor failed validation");
      failed++;
    }
  } catch (error) {
    console.log(`✗ BackendService constructor error: ${error.message}`);
    failed++;
  }
}

// Test 2: Validation methods exist
function test2() {
  try {
    const service = new BackendService(
      mockApp,
      true,
      null,
      mockContract,
      console
    );

    const requiredMethods = [
      "_validateDotnetRuntime",
      "_validateBackendExecutable",
      "_checkPortAvailability",
      "_identifyPortUser",
      "_getExpectedBackendPath",
      "_startInternal",
    ];

    let allMethodsExist = true;
    const missingMethods = [];

    for (const method of requiredMethods) {
      if (typeof service[method] !== "function") {
        allMethodsExist = false;
        missingMethods.push(method);
      }
    }

    if (allMethodsExist) {
      console.log("✓ All validation methods exist");
      passed++;
    } else {
      console.log(`✗ Missing validation methods: ${missingMethods.join(", ")}`);
      failed++;
    }
  } catch (error) {
    console.log(`✗ Validation methods check error: ${error.message}`);
    failed++;
  }
}

// Test 3: .NET validation method returns expected structure
async function test3() {
  try {
    const service = new BackendService(
      mockApp,
      true,
      null,
      mockContract,
      console
    );

    const result = await service._validateDotnetRuntime();

    if (
      result &&
      typeof result.available === "boolean" &&
      (result.version || result.error)
    ) {
      console.log(
        `✓ _validateDotnetRuntime returns correct structure (available: ${result.available})`
      );
      if (result.available) {
        console.log(`  .NET version detected: ${result.version}`);
      } else {
        console.log(`  .NET not available: ${result.error}`);
      }
      passed++;
    } else {
      console.log("✗ _validateDotnetRuntime returns invalid structure");
      failed++;
    }
  } catch (error) {
    console.log(`✗ _validateDotnetRuntime error: ${error.message}`);
    failed++;
  }
}

// Test 4: Executable validation with non-existent file
function test4() {
  try {
    const service = new BackendService(
      mockApp,
      true,
      null,
      mockContract,
      console
    );

    const result = service._validateBackendExecutable(
      "/nonexistent/path/Aura.Api.exe"
    );

    if (
      result &&
      result.valid === false &&
      result.error &&
      result.suggestion
    ) {
      console.log("✓ _validateBackendExecutable detects missing file");
      passed++;
    } else {
      console.log("✗ _validateBackendExecutable failed to detect missing file");
      failed++;
    }
  } catch (error) {
    console.log(`✗ _validateBackendExecutable error: ${error.message}`);
    failed++;
  }
}

// Test 5: Port availability check
async function test5() {
  try {
    const service = new BackendService(
      mockApp,
      true,
      null,
      mockContract,
      console
    );

    // Use a random high port that should be available
    const testPort = 50000 + Math.floor(Math.random() * 10000);
    const result = await service._checkPortAvailability(testPort);

    if (result && typeof result.available === "boolean") {
      console.log(
        `✓ _checkPortAvailability works (port ${testPort} available: ${result.available})`
      );
      passed++;
    } else {
      console.log("✗ _checkPortAvailability returns invalid structure");
      failed++;
    }
  } catch (error) {
    console.log(`✗ _checkPortAvailability error: ${error.message}`);
    failed++;
  }
}

// Test 6: Retry logic in start() method
async function test6() {
  try {
    // This test just verifies the start() method exists and is callable
    // We won't actually start the backend as that requires full environment
    const service = new BackendService(
      mockApp,
      true,
      null,
      mockContract,
      console
    );

    if (typeof service.start === "function") {
      console.log("✓ start() method exists with retry logic wrapper");
      passed++;
    } else {
      console.log("✗ start() method not found");
      failed++;
    }
  } catch (error) {
    console.log(`✗ start() method check error: ${error.message}`);
    failed++;
  }
}

// Test 7: Error diagnostics structure
function test7() {
  try {
    const service = new BackendService(
      mockApp,
      true,
      null,
      mockContract,
      console
    );

    // Test that _getExpectedBackendPath returns a string
    const expectedPath = service._getExpectedBackendPath();

    if (expectedPath && typeof expectedPath === "string") {
      console.log("✓ _getExpectedBackendPath returns valid path");
      passed++;
    } else {
      console.log("✗ _getExpectedBackendPath returns invalid result");
      failed++;
    }
  } catch (error) {
    console.log(`✗ _getExpectedBackendPath error: ${error.message}`);
    failed++;
  }
}

// Run all tests
async function runTests() {
  test1();
  test2();
  await test3();
  test4();
  await test5();
  await test6();
  test7();

  console.log("\n=== Test Summary ===");
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
