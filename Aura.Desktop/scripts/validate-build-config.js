#!/usr/bin/env node
/**
 * Validation script for electron-builder configuration
 * Ensures only Windows builds are configured
 */

const fs = require('fs');
const path = require('path');

const packageJsonPath = path.join(__dirname, '..', 'package.json');
const packageJson = JSON.parse(fs.readFileSync(packageJsonPath, 'utf8'));

console.log('Validating electron-builder configuration...\n');

let hasErrors = false;

// Check that build configuration exists
if (!packageJson.build) {
  console.error('❌ ERROR: No build configuration found in package.json');
  hasErrors = true;
}

// Check that mac is null or undefined
if (packageJson.build.mac !== null && packageJson.build.mac !== undefined) {
  console.error('❌ ERROR: macOS builds must be disabled (set "mac": null)');
  console.error(`   Current value: ${JSON.stringify(packageJson.build.mac)}`);
  hasErrors = true;
} else {
  console.log('✅ macOS builds are disabled');
}

// Check that linux is null or undefined
if (packageJson.build.linux !== null && packageJson.build.linux !== undefined) {
  console.error('❌ ERROR: Linux builds must be disabled (set "linux": null)');
  console.error(`   Current value: ${JSON.stringify(packageJson.build.linux)}`);
  hasErrors = true;
} else {
  console.log('✅ Linux builds are disabled');
}

// Check that win configuration exists
if (!packageJson.build.win) {
  console.error('❌ ERROR: Windows build configuration is missing');
  hasErrors = true;
} else {
  console.log('✅ Windows build configuration exists');
  
  // Check Windows targets
  if (!packageJson.build.win.target || !Array.isArray(packageJson.build.win.target)) {
    console.error('❌ ERROR: Windows targets are not properly configured');
    hasErrors = true;
  } else {
    const targets = packageJson.build.win.target.map(t => t.target);
    console.log(`   Targets: ${targets.join(', ')}`);
    
    // Verify only x64 architecture
    const allX64 = packageJson.build.win.target.every(t => 
      t.arch && t.arch.length === 1 && t.arch[0] === 'x64'
    );
    
    if (allX64) {
      console.log('✅ All Windows targets use x64 architecture');
    } else {
      console.error('❌ ERROR: Not all Windows targets are configured for x64');
      hasErrors = true;
    }
  }
  
  // Check that certificate file is not specified
  if (packageJson.build.win.certificateFile) {
    console.error('❌ ERROR: certificateFile should not be specified in package.json');
    console.error('   Certificate handling should be done via environment variables and sign script');
    hasErrors = true;
  } else {
    console.log('✅ Certificate file not specified (will use environment variables)');
  }
}

// Check that scripts are configured correctly
const scriptsToCheck = {
  'build': 'electron-builder build --win',
  'build:win': 'electron-builder build --win'
};

for (const [scriptName, expectedCommand] of Object.entries(scriptsToCheck)) {
  if (!packageJson.scripts[scriptName]) {
    console.error(`❌ ERROR: Script "${scriptName}" is missing`);
    hasErrors = true;
  } else if (packageJson.scripts[scriptName] !== expectedCommand) {
    console.error(`❌ ERROR: Script "${scriptName}" has incorrect command`);
    console.error(`   Expected: ${expectedCommand}`);
    console.error(`   Actual: ${packageJson.scripts[scriptName]}`);
    hasErrors = true;
  }
}

if (!hasErrors) {
  console.log('\n✅ All validation checks passed!');
  console.log('\nBuild configuration is correctly set for Windows-only builds.');
  process.exit(0);
} else {
  console.log('\n❌ Validation failed! Please fix the errors above.');
  process.exit(1);
}
