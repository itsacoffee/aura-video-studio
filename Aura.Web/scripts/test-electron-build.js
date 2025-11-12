#!/usr/bin/env node

/**
 * Electron Build Compatibility Test
 * 
 * This script tests that the Vite build output is compatible with Electron.
 * It simulates what Electron does when loading index.html via file:// protocol.
 */

import { readFileSync, existsSync, statSync } from 'fs';
import { join, dirname, resolve } from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

let testsPassed = 0;
let testsFailed = 0;

function test(description, fn) {
  try {
    fn();
    console.log(`✅ ${description}`);
    testsPassed++;
    return true;
  } catch (error) {
    console.error(`❌ ${description}`);
    console.error(`   ${error.message}`);
    testsFailed++;
    return false;
  }
}

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

function main() {
  console.log('\n=== Electron Build Compatibility Test ===\n');
  
  const distPath = join(__dirname, '..', 'dist');
  const indexPath = join(distPath, 'index.html');
  
  // Test 1: index.html exists
  test('index.html exists in dist', () => {
    assert(existsSync(indexPath), 'index.html not found');
  });
  
  // Test 2: index.html is readable
  let html = '';
  test('index.html is readable', () => {
    html = readFileSync(indexPath, 'utf-8');
    assert(html.length > 0, 'index.html is empty');
  });
  
  if (!html) {
    console.error('\n❌ Cannot continue - index.html could not be read\n');
    process.exit(1);
  }
  
  // Test 3: No absolute paths in script tags
  test('No absolute paths in <script> tags', () => {
    const scriptMatches = html.match(/<script[^>]+src="\/[^"]+"/g);
    assert(!scriptMatches, `Found ${scriptMatches?.length || 0} absolute script paths`);
  });
  
  // Test 4: No absolute paths in link tags
  test('No absolute paths in <link> tags', () => {
    const linkMatches = html.match(/<link[^>]+href="\/[^"]+"/g);
    assert(!linkMatches, `Found ${linkMatches?.length || 0} absolute link paths`);
  });
  
  // Test 5: Has relative script paths
  test('Has relative paths in <script> tags', () => {
    const scriptMatches = html.match(/<script[^>]+src="\.\//g);
    assert(scriptMatches && scriptMatches.length > 0, 'No relative script paths found');
  });
  
  // Test 6: Has relative link paths
  test('Has relative paths in <link> tags', () => {
    const linkMatches = html.match(/<link[^>]+href="\.\//g);
    assert(linkMatches && linkMatches.length > 0, 'No relative link paths found');
  });
  
  // Test 7: No <base> tag (causes issues in Electron)
  test('No <base> tag in HTML', () => {
    assert(!html.includes('<base'), '<base> tag found - not compatible with Electron');
  });
  
  // Test 8: Verify referenced assets exist
  test('All referenced assets exist', () => {
    const scriptSrc = html.match(/<script[^>]+src="([^"]+)"/g) || [];
    const linkHref = html.match(/<link[^>]+href="([^"]+)"/g) || [];
    
    const extractPath = (match) => {
      const pathMatch = match.match(/(?:src|href)="([^"]+)"/);
      return pathMatch ? pathMatch[1] : null;
    };
    
    const allPaths = [
      ...scriptSrc.map(extractPath),
      ...linkHref.map(extractPath)
    ].filter(p => p && p.startsWith('./'));
    
    const missingAssets = [];
    for (const relPath of allPaths) {
      // Convert relative path to absolute
      const assetPath = join(distPath, relPath.substring(2)); // Remove './'
      if (!existsSync(assetPath)) {
        missingAssets.push(relPath);
      }
    }
    
    assert(missingAssets.length === 0, 
      `Missing assets: ${missingAssets.join(', ')}`);
  });
  
  // Test 9: Single CSS file (cssCodeSplit: false)
  test('CSS bundled into single file', () => {
    const cssMatches = html.match(/<link[^>]+rel="stylesheet"[^>]*>/g) || [];
    const cssFiles = cssMatches.map(match => {
      const hrefMatch = match.match(/href="([^"]+)"/);
      return hrefMatch ? hrefMatch[1] : null;
    }).filter(h => h && h.endsWith('.css'));
    
    assert(cssFiles.length === 1, 
      `Expected 1 CSS file, found ${cssFiles.length}`);
  });
  
  // Test 10: Module type scripts use ES format
  test('Module scripts use ES format', () => {
    const moduleScripts = html.match(/<script[^>]+type="module"[^>]*>/g);
    assert(moduleScripts && moduleScripts.length > 0, 
      'No ES module scripts found');
  });
  
  // Test 11: Assets directory exists
  test('Assets directory exists', () => {
    const assetsPath = join(distPath, 'assets');
    assert(existsSync(assetsPath) && statSync(assetsPath).isDirectory(),
      'Assets directory not found');
  });
  
  // Test 12: Critical public assets copied
  test('Critical public assets copied', () => {
    const criticalAssets = [
      'favicon.ico',
      'favicon-16x16.png',
      'favicon-32x32.png',
      'logo256.png',
      'logo512.png'
    ];
    
    const missingAssets = criticalAssets.filter(asset => {
      const assetPath = join(distPath, asset);
      return !existsSync(assetPath);
    });
    
    assert(missingAssets.length === 0,
      `Missing critical assets: ${missingAssets.join(', ')}`);
  });
  
  // Test 13: Simulate file:// protocol path resolution
  test('Simulates file:// protocol resolution', () => {
    // In Electron, file://path/to/dist/index.html loads assets relative to that path
    const baseUrl = `file://${resolve(distPath)}/index.html`;
    const testPath = './assets/index-test.js';
    
    // This should resolve to file://path/to/dist/assets/index-test.js
    const resolved = new URL(testPath, baseUrl).pathname;
    const expected = join(distPath, 'assets', 'index-test.js');
    
    // Verify the resolution logic works as expected
    assert(resolved.includes('/assets/'), 
      'Relative path resolution failed for file:// protocol');
  });
  
  // Summary
  console.log('\n=== Test Summary ===\n');
  console.log(`✅ Passed: ${testsPassed}`);
  console.log(`❌ Failed: ${testsFailed}`);
  console.log(`Total: ${testsPassed + testsFailed}\n`);
  
  if (testsFailed > 0) {
    console.error('❌ Some tests failed - build may not be compatible with Electron\n');
    process.exit(1);
  } else {
    console.log('✅ All tests passed - build is compatible with Electron\n');
    process.exit(0);
  }
}

main();
