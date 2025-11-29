#!/usr/bin/env node
/**
 * Convert workspace:* dependencies to * for npm workspace compatibility.
 *
 * This script is needed because npm does not support the workspace:* protocol
 * that bun and pnpm use. For npm workspaces, we simply use "*" which will
 * resolve to the local workspace package.
 *
 * It also patches:
 * - auth package to include drizzle-orm (for better-auth peer dependency)
 * - root package.json to include react/react-dom (for next.js peer dependency)
 *
 * Usage: node scripts/convert-workspace-deps.js [--restore]
 *   --restore: Restore original package.json files from backup
 */

const fs = require("fs");
const path = require("path");

const OPENCUT_ROOT = path.join(__dirname, "..", "..", "OpenCut");

// List of package.json files that may contain workspace:* references
const PACKAGE_FILES = [
  "package.json",
  "apps/web/package.json",
  "apps/transcription/package.json",
  "packages/auth/package.json",
  "packages/db/package.json",
];

/**
 * Converts workspace:* dependencies to * for npm workspace compatibility.
 * @param {string} filePath - Path to the package.json file
 * @param {string} rootDir - Path to the OpenCut root directory
 * @returns {boolean} - Whether any changes were made
 */
function convertWorkspaceDeps(filePath, rootDir) {
  const fullPath = path.join(rootDir, filePath);

  if (!fs.existsSync(fullPath)) {
    console.log(`  Skipping ${filePath} (not found)`);
    return false;
  }

  const content = fs.readFileSync(fullPath, "utf8");
  const pkg = JSON.parse(content);
  let modified = false;

  // Process dependencies
  for (const depType of ["dependencies", "devDependencies", "peerDependencies"]) {
    if (!pkg[depType]) continue;

    for (const [name, version] of Object.entries(pkg[depType])) {
      if (typeof version === "string" && version.startsWith("workspace:")) {
        // Convert workspace:* to * for npm workspace resolution
        pkg[depType][name] = "*";
        console.log(`  ${filePath}: ${name} workspace:* -> *`);
        modified = true;
      }
    }
  }

  // Special handling for root package.json - add react and react-dom
  // because next.js needs them and npm doesn't properly resolve peer deps
  if (filePath === "package.json") {
    if (!pkg.dependencies) {
      pkg.dependencies = {};
    }
    if (!pkg.dependencies["react"]) {
      pkg.dependencies["react"] = "^18.2.0";
      console.log(`  ${filePath}: Added react dependency for next.js`);
      modified = true;
    }
    if (!pkg.dependencies["react-dom"]) {
      pkg.dependencies["react-dom"] = "^18.2.0";
      console.log(`  ${filePath}: Added react-dom dependency for next.js`);
      modified = true;
    }
  }

  // Special handling for auth package - add drizzle-orm as dependency
  // because better-auth needs it and npm doesn't properly resolve peer deps
  // across workspace packages
  if (filePath === "packages/auth/package.json") {
    if (!pkg.dependencies) {
      pkg.dependencies = {};
    }
    if (!pkg.dependencies["drizzle-orm"]) {
      pkg.dependencies["drizzle-orm"] = "^0.44.2";
      console.log(`  ${filePath}: Added drizzle-orm dependency for better-auth`);
      modified = true;
    }
  }

  if (modified) {
    // Backup original file
    const backupPath = fullPath + ".bak";
    if (!fs.existsSync(backupPath)) {
      fs.copyFileSync(fullPath, backupPath);
    }

    // Write modified file
    fs.writeFileSync(fullPath, JSON.stringify(pkg, null, 2) + "\n", "utf8");
  }

  return modified;
}

/**
 * Restores original package.json files from backup.
 * @param {string} rootDir - Path to the OpenCut root directory
 */
function restoreBackups(rootDir) {
  console.log("Restoring original package.json files...");

  for (const file of PACKAGE_FILES) {
    const fullPath = path.join(rootDir, file);
    const backupPath = fullPath + ".bak";

    if (fs.existsSync(backupPath)) {
      fs.copyFileSync(backupPath, fullPath);
      fs.unlinkSync(backupPath);
      console.log(`  Restored ${file}`);
    }
  }

  console.log("Done.");
}

/**
 * Main function.
 */
function main() {
  const args = process.argv.slice(2);
  const restore = args.includes("--restore");

  if (!fs.existsSync(OPENCUT_ROOT)) {
    console.error(`OpenCut directory not found: ${OPENCUT_ROOT}`);
    process.exit(1);
  }

  if (restore) {
    restoreBackups(OPENCUT_ROOT);
    return;
  }

  console.log("Converting workspace:* dependencies to * for npm compatibility...");
  console.log(`OpenCut root: ${OPENCUT_ROOT}`);

  let totalModified = 0;

  for (const file of PACKAGE_FILES) {
    if (convertWorkspaceDeps(file, OPENCUT_ROOT)) {
      totalModified++;
    }
  }

  if (totalModified > 0) {
    console.log(`\nModified ${totalModified} file(s).`);
    console.log("Original files backed up with .bak extension.");
    console.log("Run with --restore to restore original files.");
  } else {
    console.log("\nNo workspace:* dependencies found. No changes made.");
  }
}

main();
