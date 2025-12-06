#!/usr/bin/env node

/**
 * Test: lodash.isequal module availability
 *
 * Purpose: Verify that lodash.isequal is correctly installed and can be loaded
 * by electron-updater and other dependencies that require it.
 *
 * Background: PR #134 attempted to replace lodash.isequal with fast-deep-equal
 * using npm alias syntax, but this caused runtime errors because the module
 * name didn't match what electron-updater was trying to require.
 *
 * This test ensures the fix is working correctly.
 */

const path = require("path");

// Test configuration
const TEST_NAME = "lodash.isequal Module Availability Test";
const TEST_DESCRIPTION =
  "Verifies lodash.isequal is available for electron-updater";

console.log("=".repeat(60));
console.log(TEST_NAME);
console.log("=".repeat(60));
console.log(TEST_DESCRIPTION);
console.log("=".repeat(60));
console.log();

let testsPassed = 0;
let testsFailed = 0;

/**
 * Log a test result
 */
function logTest(testName, passed, details = "") {
  if (passed) {
    console.log(`✓ ${testName}`);
    testsPassed++;
  } else {
    console.error(`✗ ${testName}`);
    if (details) {
      console.error(`  Details: ${details}`);
    }
    testsFailed++;
  }
}

/**
 * Test 1: Verify lodash.isequal can be loaded
 */
function testLodashIsEqualCanBeLoaded() {
  try {
    const isEqual = require("lodash.isequal");
    logTest(
      "lodash.isequal can be required",
      typeof isEqual === "function",
      typeof isEqual !== "function" ? `Got ${typeof isEqual}` : ""
    );
    return isEqual;
  } catch (error) {
    logTest("lodash.isequal can be required", false, error.message);
    return null;
  }
}

/**
 * Test 2: Verify lodash.isequal works correctly
 */
function testLodashIsEqualFunctionality(isEqual) {
  if (!isEqual) {
    logTest("lodash.isequal basic functionality", false, "Module not loaded");
    return;
  }

  try {
    const obj1 = { a: 1, b: 2, c: { d: 3 } };
    const obj2 = { a: 1, b: 2, c: { d: 3 } };
    const obj3 = { a: 1, b: 2, c: { d: 4 } };

    const result1 = isEqual(obj1, obj2);
    const result2 = isEqual(obj1, obj3);

    logTest(
      "lodash.isequal basic functionality",
      result1 === true && result2 === false,
      result1 !== true || result2 !== false
        ? `Expected (true, false), got (${result1}, ${result2})`
        : ""
    );
  } catch (error) {
    logTest("lodash.isequal basic functionality", false, error.message);
  }
}

/**
 * Test 3: Verify electron-updater can load lodash.isequal
 */
function testElectronUpdaterCanLoadDependency() {
  try {
    // Try to load electron-updater (which depends on lodash.isequal)
    // We just verify the module loads without "Cannot find module" errors
    // Note: electron-updater requires Electron app context, so it may throw
    // other errors when used outside of Electron, but as long as it doesn't
    // throw "Cannot find module 'lodash.isequal'", the fix is working
    require("electron-updater");

    logTest(
      "electron-updater can load with lodash.isequal dependency",
      true,
      ""
    );
  } catch (error) {
    // Check if the error is specifically about lodash.isequal being missing
    const isMissingLodash =
      error.message && error.message.includes("lodash.isequal");

    if (isMissingLodash) {
      logTest(
        "electron-updater can load with lodash.isequal dependency",
        false,
        error.message
      );
    } else {
      // Other errors (like missing app context) are expected in test environment
      // As long as it's not a missing module error, the fix is working
      logTest(
        "electron-updater can load with lodash.isequal dependency",
        true,
        `Module loaded (runtime error expected in test: ${error.message.substring(0, 50)}...)`
      );
    }
  }
}

/**
 * Test 4: Verify package.json has lodash.isequal in dependencies
 */
function testPackageJsonHasCorrectDependency() {
  try {
    const packageJson = require("../package.json");
    const hasLodashInDeps =
      packageJson.dependencies && "lodash.isequal" in packageJson.dependencies;

    logTest(
      "package.json has lodash.isequal in dependencies",
      hasLodashInDeps,
      !hasLodashInDeps ? "lodash.isequal not found in dependencies" : ""
    );
  } catch (error) {
    logTest(
      "package.json has lodash.isequal in dependencies",
      false,
      error.message
    );
  }
}

/**
 * Test 5: Verify package.json does NOT have the problematic override
 */
function testPackageJsonDoesNotHaveProblematicOverride() {
  try {
    const packageJson = require("../package.json");
    const hasProblematicOverride =
      packageJson.overrides &&
      packageJson.overrides["lodash.isequal"] &&
      packageJson.overrides["lodash.isequal"].includes("fast-deep-equal");

    logTest(
      "package.json does NOT have lodash.isequal override",
      !hasProblematicOverride,
      hasProblematicOverride
        ? "Found problematic override in package.json"
        : ""
    );
  } catch (error) {
    logTest(
      "package.json does NOT have lodash.isequal override",
      false,
      error.message
    );
  }
}

/**
 * Test 6: Verify lodash.isequal is in node_modules
 */
function testLodashIsEqualInNodeModules() {
  const fs = require("fs");
  const lodashPath = path.join(
    __dirname,
    "..",
    "node_modules",
    "lodash.isequal"
  );

  try {
    const exists = fs.existsSync(lodashPath);
    logTest(
      "lodash.isequal exists in node_modules",
      exists,
      !exists ? `Path does not exist: ${lodashPath}` : ""
    );

    if (exists) {
      // Verify package.json exists
      const packagePath = path.join(lodashPath, "package.json");
      const packageExists = fs.existsSync(packagePath);
      logTest(
        "lodash.isequal has valid package structure",
        packageExists,
        !packageExists ? `Package.json not found: ${packagePath}` : ""
      );
    }
  } catch (error) {
    logTest("lodash.isequal exists in node_modules", false, error.message);
  }
}

// Run all tests
console.log("Running tests...\n");

const isEqual = testLodashIsEqualCanBeLoaded();
testLodashIsEqualFunctionality(isEqual);
testElectronUpdaterCanLoadDependency();
testPackageJsonHasCorrectDependency();
testPackageJsonDoesNotHaveProblematicOverride();
testLodashIsEqualInNodeModules();

// Print summary
console.log();
console.log("=".repeat(60));
console.log("Test Summary");
console.log("=".repeat(60));
console.log(`Total tests: ${testsPassed + testsFailed}`);
console.log(`Passed: ${testsPassed}`);
console.log(`Failed: ${testsFailed}`);
console.log("=".repeat(60));

// Exit with appropriate code
if (testsFailed > 0) {
  console.error("\n❌ Some tests failed!");
  process.exit(1);
} else {
  console.log("\n✅ All tests passed!");
  process.exit(0);
}
