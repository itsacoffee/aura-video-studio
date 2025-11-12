#!/usr/bin/env node

/**
 * Post-build validation script for Electron compatibility
 * Ensures all paths in index.html are relative (not absolute)
 * This is critical for Electron's file:// protocol to work correctly
 */

import { readFileSync, existsSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

let hasErrors = false;
let hasWarnings = false;

function log(message, type = 'info') {
  const colors = {
    error: '\x1b[31m',
    warning: '\x1b[33m',
    success: '\x1b[32m',
    info: '\x1b[36m',
    reset: '\x1b[0m'
  };
  
  const symbols = {
    error: '✗',
    warning: '⚠',
    success: '✓',
    info: 'ℹ'
  };
  
  console.log(`${colors[type]}${symbols[type]} ${message}${colors.reset}`);
}

function validateRelativePaths() {
  console.log('\n=== Electron Compatibility: Relative Path Validation ===\n');
  
  const distPath = join(__dirname, '..', 'dist');
  const indexPath = join(distPath, 'index.html');
  
  if (!existsSync(indexPath)) {
    log('index.html not found in dist directory', 'error');
    hasErrors = true;
    return false;
  }
  
  log('Reading index.html...', 'info');
  const html = readFileSync(indexPath, 'utf-8');
  
  // Patterns to check for absolute paths
  const patterns = [
    {
      regex: /<script[^>]+src="\/[^"]+"/g,
      description: 'Absolute script src paths',
      example: 'src="/assets/index.js" should be src="./assets/index.js"'
    },
    {
      regex: /<link[^>]+href="\/[^"]+"/g,
      description: 'Absolute link href paths',
      example: 'href="/assets/style.css" should be href="./assets/style.css"'
    },
    {
      regex: /href="\/favicon/g,
      description: 'Absolute favicon paths',
      example: 'href="/favicon.ico" should be href="./favicon.ico"'
    },
    {
      regex: /href="\/logo/g,
      description: 'Absolute logo paths',
      example: 'href="/logo256.png" should be href="./logo256.png"'
    }
  ];
  
  let foundIssues = [];
  
  for (const pattern of patterns) {
    const matches = html.match(pattern.regex);
    if (matches && matches.length > 0) {
      foundIssues.push({
        description: pattern.description,
        example: pattern.example,
        matches: matches,
        count: matches.length
      });
    }
  }
  
  if (foundIssues.length > 0) {
    log('Found absolute paths in index.html (should be relative for Electron):', 'error');
    hasErrors = true;
    
    for (const issue of foundIssues) {
      console.log(`\n  ${issue.description} (${issue.count} occurrence${issue.count > 1 ? 's' : ''}):`);
      console.log(`  Example: ${issue.example}`);
      console.log('  Found:');
      issue.matches.slice(0, 3).forEach(match => {
        console.log(`    ${match}`);
      });
      if (issue.matches.length > 3) {
        console.log(`    ... and ${issue.matches.length - 3} more`);
      }
    }
    
    console.log('\n  💡 Fix: Ensure vite.config.ts has base: "./" instead of base: "/"');
    return false;
  }
  
  // Check for relative paths (positive validation)
  const relativePatterns = [
    {
      regex: /<script[^>]+src="\.\//g,
      description: 'Relative script src paths'
    },
    {
      regex: /<link[^>]+href="\.\//g,
      description: 'Relative link href paths'
    }
  ];
  
  let foundRelative = false;
  for (const pattern of relativePatterns) {
    const matches = html.match(pattern.regex);
    if (matches && matches.length > 0) {
      log(`${pattern.description}: ${matches.length} found`, 'success');
      foundRelative = true;
    }
  }
  
  if (!foundRelative) {
    log('No script or link tags found with relative paths', 'warning');
    log('This might indicate an issue with the build output', 'warning');
    hasWarnings = true;
  }
  
  // Additional checks for Electron-specific concerns
  console.log('\n--- Additional Electron Compatibility Checks ---\n');
  
  // Check for inline scripts (good for Electron)
  const inlineScriptMatches = html.match(/<script[^>]*>[\s\S]*?<\/script>/g);
  if (inlineScriptMatches) {
    const nonModuleInline = inlineScriptMatches.filter(s => !s.includes('type="module"'));
    if (nonModuleInline.length > 0) {
      log(`Found ${nonModuleInline.length} inline script(s) (compatible with Electron)`, 'success');
    }
  }
  
  // Check for data URIs (also good for Electron)
  const dataUriMatches = html.match(/href="data:/g);
  if (dataUriMatches) {
    log(`Found ${dataUriMatches.length} data URI(s) (compatible with Electron)`, 'success');
  }
  
  // Check base tag (should not exist for Electron)
  if (html.includes('<base')) {
    log('Found <base> tag in HTML (may cause issues in Electron)', 'warning');
    log('Consider removing <base> tag for Electron compatibility', 'warning');
    hasWarnings = true;
  } else {
    log('No <base> tag found (good for Electron)', 'success');
  }
  
  return !hasErrors;
}

function main() {
  const success = validateRelativePaths();
  
  console.log('\n=== Validation Summary ===\n');
  
  if (hasErrors) {
    log('Relative path validation FAILED', 'error');
    log('The build output is NOT compatible with Electron', 'error');
    console.log('\nThe index.html file contains absolute paths that will not work with Electron\'s file:// protocol.');
    console.log('Please update vite.config.ts to use base: "./" instead of base: "/"');
    process.exit(1);
  } else if (hasWarnings) {
    log('Relative path validation passed with warnings', 'warning');
    log('Build output should work with Electron but has some concerns', 'warning');
    process.exit(0);
  } else {
    log('Relative path validation PASSED', 'success');
    log('Build output is compatible with Electron', 'success');
    process.exit(0);
  }
}

main();
