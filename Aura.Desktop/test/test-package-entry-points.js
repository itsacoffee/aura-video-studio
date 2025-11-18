#!/usr/bin/env node
/**
 * Test: Package.json entry point configuration
 * 
 * This test verifies that package.json correctly enforces the modular
 * Electron entry points and includes proper documentation.
 */

const fs = require('fs');
const path = require('path');

console.log('Testing package.json entry point configuration...\n');

const packageJsonPath = path.join(__dirname, '..', 'package.json');
const packageJson = JSON.parse(fs.readFileSync(packageJsonPath, 'utf8'));

console.log('='.repeat(70));
console.log('Test 1: Main Entry Point');
console.log('='.repeat(70));

if (packageJson.main === 'electron/main.js') {
  console.log('✅ Main entry point is correct: electron/main.js');
} else {
  console.error('❌ FAIL: Main entry point is incorrect');
  console.error(`   Expected: electron/main.js`);
  console.error(`   Actual: ${packageJson.main}`);
  process.exit(1);
}

console.log('\n' + '='.repeat(70));
console.log('Test 2: AuraMeta Documentation');
console.log('='.repeat(70));

if (packageJson.auraMeta) {
  console.log('✅ auraMeta section exists');
  
  if (packageJson.auraMeta.entryPoints) {
    console.log('✅ auraMeta.entryPoints section exists');
    
    if (packageJson.auraMeta.entryPoints.main === 'electron/main.js') {
      console.log('✅ auraMeta documents correct main entry');
    } else {
      console.error('❌ FAIL: auraMeta main entry is incorrect');
      process.exit(1);
    }
    
    if (packageJson.auraMeta.entryPoints.preload === 'electron/preload.js') {
      console.log('✅ auraMeta documents correct preload entry');
    } else {
      console.error('❌ FAIL: auraMeta preload entry is incorrect');
      process.exit(1);
    }
    
    if (packageJson.auraMeta.entryPoints.notes && 
        Array.isArray(packageJson.auraMeta.entryPoints.notes) &&
        packageJson.auraMeta.entryPoints.notes.length > 0) {
      console.log('✅ auraMeta includes documentation notes');
      
      const notes = packageJson.auraMeta.entryPoints.notes.join(' ');
      
      if (notes.includes('electron/main.js')) {
        console.log('✅ Notes mention electron/main.js');
      } else {
        console.warn('⚠️  WARNING: Notes do not mention electron/main.js');
      }
      
      if (notes.includes('canonical') || notes.includes('ONLY')) {
        console.log('✅ Notes emphasize canonical/exclusive nature');
      } else {
        console.warn('⚠️  WARNING: Notes should emphasize canonical entry points');
      }
      
      if (notes.includes('legacy') || notes.includes('electron.js')) {
        console.log('✅ Notes mention legacy files');
      } else {
        console.warn('⚠️  WARNING: Notes should mention legacy files');
      }
    } else {
      console.error('❌ FAIL: auraMeta.entryPoints.notes is missing or empty');
      process.exit(1);
    }
  } else {
    console.error('❌ FAIL: auraMeta.entryPoints section is missing');
    process.exit(1);
  }
} else {
  console.error('❌ FAIL: auraMeta section is missing');
  process.exit(1);
}

console.log('\n' + '='.repeat(70));
console.log('Test 3: Build Configuration');
console.log('='.repeat(70));

if (packageJson.build && packageJson.build.files) {
  console.log('✅ build.files configuration exists');
  
  const files = packageJson.build.files;
  
  // Check if electron/ directory is included
  const includesElectronDir = files.some(pattern => 
    pattern.includes('electron/**') || pattern === 'electron/**/*'
  );
  
  if (includesElectronDir) {
    console.log('✅ Build includes electron/ directory');
  } else {
    console.error('❌ FAIL: Build does not include electron/ directory');
    process.exit(1);
  }
  
  // Check if electron.js is excluded
  const excludesElectronJs = files.some(pattern => 
    pattern === '!electron.js' || pattern.includes('!electron.js')
  );
  
  if (excludesElectronJs) {
    console.log('✅ Build excludes legacy electron.js');
  } else {
    console.error('❌ FAIL: Build does not explicitly exclude electron.js');
    console.error('   Add "!electron.js" to build.files array');
    process.exit(1);
  }
} else {
  console.error('❌ FAIL: build.files configuration is missing');
  process.exit(1);
}

console.log('\n' + '='.repeat(70));
console.log('Test 4: Scripts Configuration');
console.log('='.repeat(70));

const criticalScripts = {
  'start': 'electron .',
  'dev': 'electron . --dev'
};

for (const [scriptName, expectedCommand] of Object.entries(criticalScripts)) {
  if (packageJson.scripts && packageJson.scripts[scriptName]) {
    if (packageJson.scripts[scriptName] === expectedCommand) {
      console.log(`✅ Script "${scriptName}" is correctly configured`);
    } else {
      console.warn(`⚠️  WARNING: Script "${scriptName}" differs from expected`);
      console.warn(`   Expected: ${expectedCommand}`);
      console.warn(`   Actual: ${packageJson.scripts[scriptName]}`);
    }
  } else {
    console.error(`❌ FAIL: Script "${scriptName}" is missing`);
    process.exit(1);
  }
}

// Check that no script references electron.js directly
const scriptsStr = JSON.stringify(packageJson.scripts);
if (scriptsStr.includes('electron.js')) {
  console.error('❌ FAIL: Scripts reference legacy electron.js');
  console.error('   No script should reference electron.js directly');
  process.exit(1);
} else {
  console.log('✅ No scripts reference legacy electron.js');
}

console.log('\n' + '='.repeat(70));
console.log('Test 5: Test Scripts');
console.log('='.repeat(70));

if (packageJson.scripts['test:legacy-guard']) {
  console.log('✅ test:legacy-guard script exists');
} else {
  console.warn('⚠️  WARNING: test:legacy-guard script is missing');
}

if (packageJson.scripts['test:legacy-preload']) {
  console.log('✅ test:legacy-preload script exists');
} else {
  console.warn('⚠️  WARNING: test:legacy-preload script is missing');
}

if (packageJson.scripts.test) {
  const testScript = packageJson.scripts.test;
  if (testScript.includes('test-legacy-electron-guard') && 
      testScript.includes('test-legacy-preload-redirect')) {
    console.log('✅ Main test script includes legacy tests');
  } else {
    console.warn('⚠️  WARNING: Main test script does not include legacy tests');
  }
}

console.log('\n' + '='.repeat(70));
console.log('Test Summary');
console.log('='.repeat(70));
console.log('✅ All critical checks passed!');
console.log('   package.json correctly enforces modular entry points');
console.log('   and includes comprehensive documentation.');

process.exit(0);
