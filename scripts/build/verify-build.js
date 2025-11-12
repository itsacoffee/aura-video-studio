#!/usr/bin/env node

/**
 * Post-build verification script
 * Validates that the build output contains all expected artifacts
 * and meets quality standards.
 */

import { existsSync, statSync, readdirSync } from 'fs';
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

function checkFileExists(path, description) {
  if (existsSync(path)) {
    log(`${description} exists`, 'success');
    return true;
  } else {
    log(`${description} not found: ${path}`, 'error');
    hasErrors = true;
    return false;
  }
}

function checkDirectoryExists(path, description) {
  if (existsSync(path) && statSync(path).isDirectory()) {
    log(`${description} exists`, 'success');
    return true;
  } else {
    log(`${description} not found: ${path}`, 'error');
    hasErrors = true;
    return false;
  }
}

function getDirectorySize(dirPath) {
  let totalSize = 0;
  
  function traverse(currentPath) {
    try {
      const stats = statSync(currentPath);
      
      if (stats.isFile()) {
        totalSize += stats.size;
      } else if (stats.isDirectory()) {
        const files = readdirSync(currentPath);
        for (const file of files) {
          traverse(join(currentPath, file));
        }
      }
    } catch (error) {
      // Ignore errors for inaccessible files
    }
  }
  
  traverse(dirPath);
  return totalSize;
}

function formatBytes(bytes) {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(2)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
}

function countFiles(dirPath) {
  let count = 0;
  
  function traverse(currentPath) {
    try {
      const stats = statSync(currentPath);
      
      if (stats.isFile()) {
        count++;
      } else if (stats.isDirectory()) {
        const files = readdirSync(currentPath);
        for (const file of files) {
          traverse(join(currentPath, file));
        }
      }
    } catch (error) {
      // Ignore errors for inaccessible files
    }
  }
  
  traverse(dirPath);
  return count;
}

function checkNoSourceFilesInDist(distPath) {
  const sourceExtensions = ['.ts', '.tsx', '.jsx'];
  let foundSourceFiles = [];
  
  function traverse(currentPath) {
    try {
      const stats = statSync(currentPath);
      
      if (stats.isFile()) {
        for (const ext of sourceExtensions) {
          if (currentPath.endsWith(ext)) {
            foundSourceFiles.push(currentPath.replace(distPath, ''));
          }
        }
      } else if (stats.isDirectory()) {
        const files = readdirSync(currentPath);
        for (const file of files) {
          traverse(join(currentPath, file));
        }
      }
    } catch (error) {
      // Ignore errors for inaccessible files
    }
  }
  
  traverse(distPath);
  
  if (foundSourceFiles.length > 0) {
    log('Source files found in dist (should be compiled):', 'warning');
    foundSourceFiles.forEach(file => log(`  ${file}`, 'warning'));
    hasWarnings = true;
    return false;
  }
  
  log('No source files in dist', 'success');
  return true;
}

function checkNoNodeModulesInDist(distPath) {
  const nodeModulesPath = join(distPath, 'node_modules');
  
  if (existsSync(nodeModulesPath)) {
    log('node_modules found in dist (should not be copied)', 'error');
    hasErrors = true;
    return false;
  }
  
  log('No node_modules in dist', 'success');
  return true;
}

function verifyFrontendBuild() {
  console.log('\n=== Frontend Build Verification ===\n');
  
  const webRoot = join(__dirname, '..', '..', 'Aura.Web');
  const distPath = join(webRoot, 'dist');
  
  if (!checkDirectoryExists(distPath, 'Frontend dist directory')) {
    return false;
  }
  
  checkFileExists(join(distPath, 'index.html'), 'index.html');
  checkDirectoryExists(join(distPath, 'assets'), 'Assets directory');
  
  // Verify critical static assets from public folder
  const criticalAssets = [
    'favicon.ico',
    'favicon-16x16.png', 
    'favicon-32x32.png',
    'logo256.png',
    'logo512.png',
    'vite.svg'
  ];
  
  console.log('\n--- Verifying Critical Assets ---\n');
  for (const asset of criticalAssets) {
    checkFileExists(join(distPath, asset), asset);
  }
  
  // Verify workspace templates directory
  checkDirectoryExists(join(distPath, 'workspaces'), 'Workspaces directory');
  if (existsSync(join(distPath, 'workspaces', 'templates'))) {
    log('Workspace templates directory exists', 'success');
  } else {
    log('Workspace templates directory not found', 'warning');
    hasWarnings = true;
  }
  
  checkNoSourceFilesInDist(distPath);
  checkNoNodeModulesInDist(distPath);
  
  const fileCount = countFiles(distPath);
  const totalSize = getDirectorySize(distPath);
  
  log(`Total files: ${fileCount}`, 'info');
  log(`Total size: ${formatBytes(totalSize)}`, 'info');
  
  if (totalSize < 1024) {
    log('Build output seems too small (< 1 KB)', 'warning');
    hasWarnings = true;
  }
  
  return !hasErrors;
}

function main() {
  console.log('\n=== Build Output Validation ===\n');
  
  verifyFrontendBuild();
  
  console.log('\n=== Validation Summary ===\n');
  
  if (hasErrors) {
    log('Build verification failed', 'error');
    log('Build output is incomplete or invalid', 'error');
    process.exit(1);
  } else if (hasWarnings) {
    log('Build verification passed with warnings', 'warning');
    log('Build output is valid but has some concerns', 'warning');
    process.exit(0);
  } else {
    log('Build verification passed', 'success');
    log('Build output is valid and complete', 'success');
    process.exit(0);
  }
}

main();
