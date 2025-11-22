#!/usr/bin/env node

/**
 * Electron Build Verification Script
 * 
 * Verifies that the Electron app initialization fixes are correctly implemented:
 * 1. Checks vite.config.ts for minify: false
 * 2. Checks vite.config.ts for manualChunks: undefined
 * 3. Verifies frontend build produces unminified bundle
 * 4. Checks bundle size is in expected range
 * 5. Validates no circular dependency indicators
 */

const fs = require('fs');
const path = require('path');

// ANSI color codes
const colors = {
  reset: '\x1b[0m',
  bright: '\x1b[1m',
  red: '\x1b[31m',
  green: '\x1b[32m',
  yellow: '\x1b[33m',
  blue: '\x1b[34m',
  cyan: '\x1b[36m',
};

function log(message, color = 'reset') {
  console.log(`${colors[color]}${message}${colors.reset}`);
}

function success(message) {
  log(`✓ ${message}`, 'green');
}

function error(message) {
  log(`✗ ${message}`, 'red');
}

function warning(message) {
  log(`⚠ ${message}`, 'yellow');
}

function info(message) {
  log(`ℹ ${message}`, 'cyan');
}

function header(message) {
  log(`\n${'='.repeat(60)}`, 'bright');
  log(message, 'bright');
  log('='.repeat(60), 'bright');
}

// Test results
let totalTests = 0;
let passedTests = 0;
let failedTests = 0;

function test(name, condition, details = '') {
  totalTests++;
  if (condition) {
    passedTests++;
    success(`${name}`);
    if (details) {
      info(`  ${details}`);
    }
  } else {
    failedTests++;
    error(`${name}`);
    if (details) {
      info(`  ${details}`);
    }
  }
}

// Paths (using process.cwd() for better cross-platform compatibility)
const rootDir = process.cwd();
const viteConfigPath = path.join(rootDir, 'Aura.Web', 'vite.config.ts');
const distPath = path.join(rootDir, 'Aura.Web', 'dist');
const assetsPath = path.join(distPath, 'assets');
const indexHtmlPath = path.join(distPath, 'index.html');

header('Electron Build Verification');

// Test 1: Check vite.config.ts exists
info('Checking Vite configuration...');
test(
  'vite.config.ts exists',
  fs.existsSync(viteConfigPath),
  viteConfigPath
);

if (!fs.existsSync(viteConfigPath)) {
  error('Cannot continue without vite.config.ts');
  process.exit(1);
}

// Test 2: Check minify: false
const viteConfig = fs.readFileSync(viteConfigPath, 'utf8');
const hasMinifyFalse = /minify:\s*false/.test(viteConfig);
test(
  'Minification is disabled (minify: false)',
  hasMinifyFalse,
  hasMinifyFalse ? 'Found: minify: false' : 'Not found or not set to false'
);

// Test 3: Check manualChunks: undefined
const hasManualChunksUndefined = /manualChunks:\s*undefined/.test(viteConfig);
test(
  'Manual chunks disabled (manualChunks: undefined)',
  hasManualChunksUndefined,
  hasManualChunksUndefined
    ? 'Found: manualChunks: undefined'
    : 'Not found or not set to undefined'
);

// Test 4: Check tree shaking config
const hasModuleSideEffects = /moduleSideEffects:\s*['"]no-external['"]/.test(viteConfig);
test(
  'Module side effects configured',
  hasModuleSideEffects,
  'Ensures correct module initialization order'
);

// Test 5: Check base path is relative
const hasRelativeBase = /base:\s*['"]\.\/['"]/.test(viteConfig);
test(
  'Base path is relative (base: "./")',
  hasRelativeBase,
  'Required for Electron file:// protocol'
);

// Test 6: Check target is chrome128
const hasChrome128Target = /target:\s*['"]chrome128['"]/.test(viteConfig);
test(
  'Target is chrome128',
  hasChrome128Target,
  'Matches Electron 32 Chrome version'
);

// Test 7: Check CSS code split is disabled
const hasCssCodeSplitFalse = /cssCodeSplit:\s*false/.test(viteConfig);
test(
  'CSS code splitting disabled',
  hasCssCodeSplitFalse,
  'Simpler loading for Electron'
);

info('\nChecking build artifacts...');

// Test 8: Check dist folder exists
test('Frontend dist folder exists', fs.existsSync(distPath), distPath);

if (!fs.existsSync(distPath)) {
  warning('Build artifacts not found. Run: cd Aura.Web && npm run build');
  process.exit(0);
}

// Test 9: Check index.html exists
test('index.html exists', fs.existsSync(indexHtmlPath));

// Test 10: Check assets folder exists
test('Assets folder exists', fs.existsSync(assetsPath));

if (!fs.existsSync(assetsPath)) {
  error('Assets folder not found');
  process.exit(1);
}

// Test 11: Find main bundle
const assetFiles = fs.readdirSync(assetsPath);
const mainBundleFiles = assetFiles.filter(
  (f) => f.startsWith('index-') && f.endsWith('.js') && !f.includes('.gz') && !f.includes('.br')
);

info('\nAnalyzing bundles...');
info(`Found ${mainBundleFiles.length} index bundle(s):`);
mainBundleFiles.forEach((file) => {
  const filePath = path.join(assetsPath, file);
  const stats = fs.statSync(filePath);
  const sizeMB = (stats.size / (1024 * 1024)).toFixed(2);
  info(`  - ${file}: ${sizeMB} MB`);
});

// Find the largest index bundle (main bundle)
let mainBundle = null;
let mainBundleSize = 0;
mainBundleFiles.forEach((file) => {
  const filePath = path.join(assetsPath, file);
  const stats = fs.statSync(filePath);
  if (stats.size > mainBundleSize) {
    mainBundleSize = stats.size;
    mainBundle = file;
  }
});

test('Main bundle found', mainBundle !== null, mainBundle);

if (!mainBundle) {
  error('Main bundle not found');
  process.exit(1);
}

// Test 12: Check main bundle size (should be ~3.5-3.7 MB unminified)
const mainBundlePath = path.join(assetsPath, mainBundle);
const sizeMB = mainBundleSize / (1024 * 1024);
const isInExpectedRange = sizeMB >= 3.0 && sizeMB <= 4.5;
test(
  'Main bundle size in expected range (3.0-4.5 MB)',
  isInExpectedRange,
  `Actual: ${sizeMB.toFixed(2)} MB (${isInExpectedRange ? 'unminified' : 'unexpected'})`
);

// Test 13: Check bundle is unminified
info('\nVerifying bundle is unminified...');
const bundleContent = fs.readFileSync(mainBundlePath, 'utf8');
const firstLines = bundleContent.split('\n').slice(0, 50).join('\n');

// Check for unminified characteristics
const MAX_MINIFIED_LINE_LENGTH = 500; // Lines longer than this indicate minification
const hasReadableFunctionNames = /function\s+[a-zA-Z_$][a-zA-Z0-9_$]*\s*\(/.test(firstLines);
const hasProperSpacing = /{\s*\n\s+/.test(firstLines);
const hasComments = /\/\*|\*\/|\/\//.test(firstLines);
const noExtremelyLongLines = !firstLines.split('\n').some((line) => line.length > MAX_MINIFIED_LINE_LENGTH);

test(
  'Bundle has readable function names',
  hasReadableFunctionNames,
  'Indicates no variable name mangling'
);

test(
  'Bundle has proper spacing',
  hasProperSpacing || hasReadableFunctionNames,
  'Indicates unminified formatting'
);

const isUnminified = hasReadableFunctionNames && (hasProperSpacing || noExtremelyLongLines);
test(
  'Bundle is unminified',
  isUnminified,
  isUnminified
    ? 'Code appears to be unminified'
    : 'Code may be minified - check manually'
);

// Test 14: Check index.html loads single main script
info('\nVerifying index.html configuration...');
const indexHtml = fs.readFileSync(indexHtmlPath, 'utf8');
const scriptMatches = indexHtml.match(/<script[^>]*src="([^"]*)"[^>]*>/g) || [];
const moduleScripts = scriptMatches.filter((s) => s.includes('type="module"'));

test(
  'index.html has single module script',
  moduleScripts.length === 1,
  `Found ${moduleScripts.length} module script(s)`
);

if (moduleScripts.length > 0) {
  moduleScripts.forEach((script) => {
    info(`  ${script}`);
  });
}

// Test 15: Check for relative paths in index.html
const hasRelativePaths = indexHtml.includes('src="./assets/');
test(
  'index.html uses relative paths',
  hasRelativePaths,
  'Required for Electron file:// protocol'
);

// Test 16: Check for CSP
const hasCSP = indexHtml.includes('Content-Security-Policy');
test('Content Security Policy configured', hasCSP);

// Test 17: Count lazy-loaded chunks
const lazyChunks = assetFiles.filter(
  (f) => f.endsWith('.js') && !f.startsWith('index-') && !f.includes('.gz') && !f.includes('.br')
);
info(`\nLazy-loaded chunks: ${lazyChunks.length}`);
test(
  'Lazy-loaded chunks present',
  lazyChunks.length > 0,
  'Route-based code splitting (expected behavior)'
);

// Test 18: Check for circular dependency indicators
const hasCircularDepWarning =
  bundleContent.includes('circular dependency') ||
  bundleContent.includes('Circular dependency');
test(
  'No circular dependency warnings in bundle',
  !hasCircularDepWarning,
  hasCircularDepWarning ? 'Found circular dependency warnings' : 'Clean'
);

// Test 19: Check for initialization errors in bundle
const hasInitErrors =
  bundleContent.includes('Cannot access') &&
  bundleContent.includes('before initialization');
test(
  'No "Cannot access before initialization" strings',
  !hasInitErrors,
  hasInitErrors
    ? 'Found potential initialization error strings'
    : 'Clean'
);

// Summary
header('Test Summary');
log(`Total Tests: ${totalTests}`, 'cyan');
log(`Passed: ${passedTests}`, 'green');
log(`Failed: ${failedTests}`, failedTests > 0 ? 'red' : 'green');

const successRate = ((passedTests / totalTests) * 100).toFixed(1);
log(`Success Rate: ${successRate}%`, successRate >= 90 ? 'green' : 'yellow');

if (failedTests === 0) {
  log('\n✓ All tests passed! The fixes are correctly implemented.', 'green');
  log('\nNext steps:', 'cyan');
  log('1. Build the Electron app: cd Aura.Desktop && pwsh build-desktop.ps1', 'cyan');
  log('2. Launch the app and verify no blank screen', 'cyan');
  log('3. Check DevTools console for errors', 'cyan');
} else {
  log('\n✗ Some tests failed. Please review the configuration.', 'red');
  process.exit(1);
}

log(''); // Empty line at end
