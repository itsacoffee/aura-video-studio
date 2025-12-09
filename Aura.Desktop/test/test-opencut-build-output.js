#!/usr/bin/env node
/**
 * Test: OpenCut Integration Verification
 * 
 * Verifies that OpenCut is correctly integrated into Aura:
 * 
 * NATIVE INTEGRATION (Current Architecture - PR refactoring iframe to native):
 * - OpenCut runs as native React components in Aura.Web
 * - No separate server process needed
 * - No iframe loading or server health checks
 * 
 * LEGACY TESTS (Preserved but skipped):
 * - Some tests reference the old iframe/server architecture
 * - These are marked as skipped since the architecture has changed
 * 
 * This test validates the changes that refactored OpenCut from iframe-based
 * to native React component integration.
 */

const path = require('path');
const fs = require('fs');

console.log('=== OpenCut Integration Verification Tests ===\n');

let testsPassed = 0;
let testsFailed = 0;
let testsSkipped = 0;

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

function skip(name, reason) {
  console.log(`○ ${name} (skipped: ${reason})`);
  testsSkipped++;
}

// Test 1: Verify build script has native integration comment
test('build-desktop.ps1 indicates native OpenCut integration', () => {
  const buildScript = fs.readFileSync(
    path.join(__dirname, '../build-desktop.ps1'),
    'utf8'
  );
  
  // Should indicate native integration
  if (!buildScript.includes('OpenCut Native Integration') && 
      !buildScript.includes('native')) {
    throw new Error('build-desktop.ps1 should indicate native OpenCut integration');
  }
  
  // Should skip server build
  if (!buildScript.includes('SKIP_OPENCUT_SERVER_BUILD')) {
    throw new Error('build-desktop.ps1 should have SKIP_OPENCUT_SERVER_BUILD flag');
  }
});

// Test 2: Verify OpenCut manager is deprecated
test('opencut-manager.js is deprecated and returns native mode diagnostics', () => {
  const manager = fs.readFileSync(
    path.join(__dirname, '../electron/opencut-manager.js'),
    'utf8'
  );
  
  // Should be marked as deprecated
  if (!manager.includes('DEPRECATED') && !manager.includes('deprecated')) {
    throw new Error('opencut-manager.js should be marked as deprecated');
  }
  
  // Should indicate native mode in diagnostics
  if (!manager.includes('mode: "native"') || !manager.includes('native')) {
    throw new Error('opencut-manager.js getDiagnostics should indicate native mode');
  }
  
  // Server is disabled
  if (!manager.includes('enabled: false') && !manager.includes('this.enabled = false')) {
    throw new Error('opencut-manager.js should have server disabled');
  }
});

// Test 3: Verify package.json extraResources does NOT include opencut (native mode)
test('package.json extraResources excludes resources/opencut (native mode)', () => {
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
  
  if (opencutResource) {
    throw new Error('package.json extraResources should NOT include resources/opencut in native mode');
  }
});

// Test 4: Verify OpenCut page uses native components (not iframe)
test('OpenCutPage.tsx uses native OpenCutEditor component', () => {
  const opencutPagePath = path.join(__dirname, '../../Aura.Web/src/pages/OpenCutPage.tsx');
  
  if (!fs.existsSync(opencutPagePath)) {
    throw new Error('OpenCutPage.tsx not found');
  }
  
  const opencutPage = fs.readFileSync(opencutPagePath, 'utf8');
  
  // Should import OpenCutEditor
  if (!opencutPage.includes('OpenCutEditor')) {
    throw new Error('OpenCutPage.tsx should import OpenCutEditor component');
  }
  
  // Should NOT have an iframe element
  if (opencutPage.includes('<iframe')) {
    throw new Error('OpenCutPage.tsx should NOT use an iframe element in native mode');
  }
  
  // Should NOT have server health checks
  if (opencutPage.includes('checkOpenCutHealth')) {
    throw new Error('OpenCutPage.tsx should NOT have server health checks in native mode');
  }
});

// Test 5: Verify OpenCutEditor component exists
test('OpenCutEditor.tsx component exists with required features', () => {
  const editorPath = path.join(__dirname, '../../Aura.Web/src/components/OpenCut/OpenCutEditor.tsx');
  
  if (!fs.existsSync(editorPath)) {
    throw new Error('OpenCutEditor.tsx not found');
  }
  
  const editor = fs.readFileSync(editorPath, 'utf8');
  
  // Should have timeline
  if (!editor.includes('timeline') && !editor.includes('Timeline')) {
    throw new Error('OpenCutEditor should have timeline functionality');
  }
  
  // Should have preview
  if (!editor.includes('preview') && !editor.includes('Preview')) {
    throw new Error('OpenCutEditor should have preview functionality');
  }
  
  // Should have media panel
  if (!editor.includes('media') && !editor.includes('Media')) {
    throw new Error('OpenCutEditor should have media functionality');
  }
});

// Test 6: Verify OpenCut stores exist
test('OpenCut Zustand stores exist', () => {
  const storesDir = path.join(__dirname, '../../Aura.Web/src/stores');
  
  const requiredStores = [
    'opencutProject.ts',
    'opencutPlayback.ts',
    'opencutMedia.ts'
  ];
  
  for (const store of requiredStores) {
    const storePath = path.join(storesDir, store);
    if (!fs.existsSync(storePath)) {
      throw new Error(`Store ${store} not found`);
    }
  }
});

// Test 7: Verify opencut-manager.js has getDiagnostics method
test('opencut-manager.js has getDiagnostics method for troubleshooting', () => {
  const manager = fs.readFileSync(
    path.join(__dirname, '../electron/opencut-manager.js'),
    'utf8'
  );
  
  if (!manager.includes('async getDiagnostics()')) {
    throw new Error('opencut-manager.js missing getDiagnostics method');
  }
});

// Test 8: Legacy test - skipped (build script no longer copies server)
skip('build-desktop.ps1 copies server.js to resources/opencut', 
  'Native mode - server build disabled');

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
});

// Test 10: Verify resources/.gitignore ignores opencut/ directory
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
console.log(`Skipped: ${testsSkipped}`);
console.log(`Total: ${testsPassed + testsFailed + testsSkipped}`);

if (testsFailed > 0) {
  console.error('\n❌ Some tests failed');
  process.exit(1);
} else {
  console.log('\n✅ All OpenCut integration tests passed');
  console.log('\nVerified (Native Integration):');
  console.log('  - Build script indicates native OpenCut integration');
  console.log('  - OpenCut manager is deprecated (server disabled)');
  console.log('  - package.json excludes OpenCut server resources');
  console.log('  - OpenCutPage uses native OpenCutEditor component');
  console.log('  - OpenCutEditor component has timeline, preview, media');
  console.log('  - Zustand stores exist for OpenCut state');
  console.log('  - Diagnostics method available for troubleshooting');
  console.log('  - Clean script removes OpenCut workspace artifacts');
  console.log('  - Build output is properly gitignored');
  process.exit(0);
}
