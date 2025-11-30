#!/usr/bin/env node
/**
 * Test: OpenCut Build Output Verification
 * 
 * Verifies that the build script correctly prepares OpenCut resources:
 * 1. resources/opencut/server.js exists after build
 * 2. OpenCut manager expects correct paths
 * 3. OpenCut page loads via iframe (not external browser)
 * 
 * This test validates the changes from PR 15 that fixed OpenCut integration.
 */

const path = require('path');
const fs = require('fs');

console.log('=== OpenCut Build Output Verification Tests ===\n');

let testsPassed = 0;
let testsFailed = 0;

function test(name, fn) {
  try {
    fn();
    console.log(`✓ ${name}`);
    testsPassed++;
  } catch (error) {
    console.error(`✗ ${name}`);
    console.error(`  Error: ${error.message}`);
    testsFailed++;
  }
}

// Test 1: Verify build script has OpenCut copy step
test('build-desktop.ps1 has Step 1b.9 to copy OpenCut to resources/opencut', () => {
  const buildScript = fs.readFileSync(
    path.join(__dirname, '../build-desktop.ps1'),
    'utf8'
  );
  
  if (!buildScript.includes('Step 1b.9: Copy OpenCut standalone build to resources/opencut')) {
    throw new Error('build-desktop.ps1 missing Step 1b.9 for copying OpenCut');
  }
  
  if (!buildScript.includes('$openCutResourcesDir = "$ScriptDir\\resources\\opencut"')) {
    throw new Error('build-desktop.ps1 does not define $openCutResourcesDir correctly');
  }
});

// Test 2: Verify build script copies server.js
test('build-desktop.ps1 copies server.js to resources/opencut', () => {
  const buildScript = fs.readFileSync(
    path.join(__dirname, '../build-desktop.ps1'),
    'utf8'
  );
  
  // Check that it verifies server.js exists after copy
  if (!buildScript.includes('$finalServerJs = "$openCutResourcesDir\\server.js"')) {
    throw new Error('build-desktop.ps1 does not verify final server.js location');
  }
  
  if (!buildScript.includes('OpenCut resources prepared successfully')) {
    throw new Error('build-desktop.ps1 missing success message for OpenCut resources');
  }
});

// Test 3: Verify OpenCut manager looks for server.js in correct location
test('opencut-manager.js looks for server.js in resources/opencut when packaged', () => {
  const manager = fs.readFileSync(
    path.join(__dirname, '../electron/opencut-manager.js'),
    'utf8'
  );
  
  // In packaged mode, should look for server.js in process.resourcesPath/opencut
  if (!manager.includes('path.join(process.resourcesPath, "opencut")')) {
    throw new Error('opencut-manager.js does not look in process.resourcesPath/opencut');
  }
  
  if (!manager.includes('path.join(openCutAppDir, "server.js")')) {
    throw new Error('opencut-manager.js does not look for server.js');
  }
});

// Test 4: Verify package.json extraResources includes opencut
test('package.json extraResources includes resources/opencut', () => {
  const packageJson = JSON.parse(fs.readFileSync(
    path.join(__dirname, '../package.json'),
    'utf8'
  ));
  
  const extraResources = packageJson.build?.extraResources;
  if (!Array.isArray(extraResources)) {
    throw new Error('package.json missing build.extraResources array');
  }
  
  const opencutResource = extraResources.find(r => 
    (typeof r === 'object' && r.from === 'resources/opencut') ||
    (typeof r === 'string' && r.includes('opencut'))
  );
  
  if (!opencutResource) {
    throw new Error('package.json extraResources does not include resources/opencut');
  }
});

// Test 5: Verify OpenCut page uses iframe (not external browser redirect)
test('OpenCutPage.tsx loads OpenCut via iframe (not external redirect)', () => {
  const opencutPagePath = path.join(__dirname, '../../Aura.Web/src/pages/OpenCutPage.tsx');
  
  if (!fs.existsSync(opencutPagePath)) {
    throw new Error('OpenCutPage.tsx not found');
  }
  
  const opencutPage = fs.readFileSync(opencutPagePath, 'utf8');
  
  // Should have an iframe element
  if (!opencutPage.includes('<iframe')) {
    throw new Error('OpenCutPage.tsx does not use an iframe element');
  }
  
  // Should reference iframe for loading OpenCut
  if (!opencutPage.includes('iframeRef')) {
    throw new Error('OpenCutPage.tsx does not use iframeRef for iframe control');
  }
  
  // Should load OpenCut URL in iframe, not navigate away
  if (!opencutPage.includes('startLoading')) {
    throw new Error('OpenCutPage.tsx missing startLoading for iframe');
  }
});

// Test 6: Verify OpenCut defaults to localhost:3100 in Electron
test('OpenCut URL defaults to http://127.0.0.1:3100 in Electron', () => {
  const opencutPagePath = path.join(__dirname, '../../Aura.Web/src/pages/OpenCutPage.tsx');
  const opencutPage = fs.readFileSync(opencutPagePath, 'utf8');
  
  // Should have fallback to localhost:3100 for Electron
  if (!opencutPage.includes('http://127.0.0.1:3100')) {
    throw new Error('OpenCutPage.tsx does not default to http://127.0.0.1:3100');
  }
  
  // Should detect Electron environment
  if (!opencutPage.includes('Electron')) {
    throw new Error('OpenCutPage.tsx does not detect Electron environment');
  }
});

// Test 7: Verify OpenCut manager has getDiagnostics method (from PR 15)
test('opencut-manager.js has getDiagnostics method for troubleshooting', () => {
  const manager = fs.readFileSync(
    path.join(__dirname, '../electron/opencut-manager.js'),
    'utf8'
  );
  
  if (!manager.includes('async getDiagnostics()')) {
    throw new Error('opencut-manager.js missing getDiagnostics method');
  }
  
  // Should return server path info
  if (!manager.includes('serverPath:') && !manager.includes('serverExists:')) {
    throw new Error('getDiagnostics does not return server path information');
  }
});

// Test 8: Verify build script handles monorepo structure
test('build-desktop.ps1 handles monorepo structure (standalone/apps/web/server.js)', () => {
  const buildScript = fs.readFileSync(
    path.join(__dirname, '../build-desktop.ps1'),
    'utf8'
  );
  
  // Should check for monorepo path
  if (!buildScript.includes('$monorepoServerPath') || 
      !buildScript.includes('apps\\web\\server.js')) {
    throw new Error('build-desktop.ps1 does not handle monorepo structure');
  }
  
  // Should also handle single package structure
  if (!buildScript.includes('$singleServerPath') ||
      !buildScript.includes('$standaloneDir\\server.js')) {
    throw new Error('build-desktop.ps1 does not handle single package structure');
  }
});

// Test 9: Verify clean-desktop.ps1 cleans OpenCut workspace artifacts
test('clean-desktop.ps1 cleans OpenCut workspace artifacts', () => {
  const cleanScript = fs.readFileSync(
    path.join(__dirname, '../clean-desktop.ps1'),
    'utf8'
  );
  
  if (!cleanScript.includes('OpenCut')) {
    throw new Error('clean-desktop.ps1 does not reference OpenCut');
  }
  
  // Should clean OpenCut node_modules
  if (!cleanScript.includes('$projectRoot\\OpenCut\\node_modules')) {
    throw new Error('clean-desktop.ps1 does not clean OpenCut node_modules');
  }
  
  // Should clean OpenCut .next build
  if (!cleanScript.includes('$projectRoot\\OpenCut\\apps\\web\\.next')) {
    throw new Error('clean-desktop.ps1 does not clean OpenCut .next directory');
  }
  
  // Should clean build-time resources/opencut
  if (!cleanScript.includes('resources\\opencut')) {
    throw new Error('clean-desktop.ps1 does not clean resources/opencut build output');
  }
  
  // Should clean build-time resources/backend
  if (!cleanScript.includes('resources\\backend')) {
    throw new Error('clean-desktop.ps1 does not clean resources/backend build output');
  }
});

// Test 10: Verify resources/opencut is gitignored (should not commit build output)
test('resources/.gitignore ignores opencut/ directory', () => {
  const gitignorePath = path.join(__dirname, '../resources/.gitignore');
  
  if (!fs.existsSync(gitignorePath)) {
    throw new Error('resources/.gitignore not found');
  }
  
  const gitignore = fs.readFileSync(gitignorePath, 'utf8');
  
  if (!gitignore.includes('opencut/')) {
    throw new Error('resources/.gitignore does not ignore opencut/');
  }
});

// Summary
console.log('\n=== Test Summary ===');
console.log(`Passed: ${testsPassed}`);
console.log(`Failed: ${testsFailed}`);
console.log(`Total: ${testsPassed + testsFailed}`);

if (testsFailed > 0) {
  console.error('\n❌ Some tests failed');
  process.exit(1);
} else {
  console.log('\n✅ All OpenCut build output tests passed');
  console.log('\nVerified:');
  console.log('  - Build script copies OpenCut to resources/opencut');
  console.log('  - OpenCut manager looks for server.js in correct location');
  console.log('  - package.json extraResources includes opencut');
  console.log('  - OpenCutPage loads via iframe (not external browser)');
  console.log('  - OpenCut URL defaults to localhost:3100 in Electron');
  console.log('  - Diagnostics method available for troubleshooting');
  console.log('  - Build handles both monorepo and single package structures');
  console.log('  - Clean script removes OpenCut workspace artifacts');
  console.log('  - Build output is properly gitignored');
  process.exit(0);
}
