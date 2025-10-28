#!/usr/bin/env node

/**
 * Comprehensive placeholder scanner
 * Searches for TODO, FIXME, FUTURE, and other placeholder markers in source code
 */

import { readFileSync, readdirSync, statSync } from 'fs';
import { join, relative } from 'path';
import { fileURLToPath } from 'url';
import { dirname } from 'path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const REPO_ROOT = join(__dirname, '..', '..');

// Patterns to search for (case-insensitive)
// These should be actual code comments, not just the words in strings
const FORBIDDEN_PATTERNS = [
  /\/\/\s*TODO\b/i,           // // TODO
  /\/\/\s*FIXME\b/i,          // // FIXME
  /\/\/\s*HACK\b/i,           // // HACK
  /\/\/\s*XXX\b/,             // // XXX
  /\/\/\s*WIP\b/i,            // // WIP
  /\/\*\s*TODO\b/i,           // /* TODO
  /\/\*\s*FIXME\b/i,          // /* FIXME
  /\/\*\s*HACK\b/i,           // /* HACK
  /\/\*\s*XXX\b/,             // /* XXX
  /\/\*\s*WIP\b/i,            // /* WIP
];

// File extensions to scan
const SCAN_EXTENSIONS = [
  '.ts', '.tsx', '.js', '.jsx',
  '.cs', '.csx',
  '.json', '.yml', '.yaml'
];

// Directories to exclude
const EXCLUDE_DIRS = [
  'node_modules',
  'dist',
  'build',
  'bin',
  'obj',
  '.git',
  '.vs',
  '.vscode',
  'coverage',
  'wwwroot',
  '.husky',
  'out'
];

// Files to exclude (by name)
const EXCLUDE_FILES = [
  'package-lock.json',
  'package.json.bak',
  '.eslintrc.cjs',
  'tsconfig.json'
];

// Special files that are allowed to have placeholders (documentation only)
const ALLOWED_FILES = [
  // All markdown files are allowed (they're documentation)
  '.md'
];

let totalFiles = 0;
let scannedFiles = 0;
let issues = [];

function shouldScanFile(filePath) {
  const fileName = filePath.split('/').pop() || '';
  
  // Exclude specific files
  if (EXCLUDE_FILES.includes(fileName)) {
    return false;
  }
  
  // Allow certain file types
  for (const ext of ALLOWED_FILES) {
    if (fileName.endsWith(ext)) {
      return false;
    }
  }
  
  // Check if file has a scannable extension
  for (const ext of SCAN_EXTENSIONS) {
    if (fileName.endsWith(ext)) {
      return true;
    }
  }
  
  return false;
}

function shouldScanDirectory(dirPath, dirName) {
  // Exclude certain directories
  if (EXCLUDE_DIRS.includes(dirName)) {
    return false;
  }
  
  // Exclude this script's directory
  if (dirPath.includes('/scripts/audit')) {
    return false;
  }
  
  return true;
}

function scanFile(filePath) {
  scannedFiles++;
  
  try {
    const content = readFileSync(filePath, 'utf8');
    const lines = content.split('\n');
    
    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];
      
      for (const pattern of FORBIDDEN_PATTERNS) {
        if (pattern.test(line)) {
          const relativePath = relative(REPO_ROOT, filePath);
          issues.push({
            file: relativePath,
            line: i + 1,
            pattern: pattern.source,
            context: line.trim()
          });
        }
      }
    }
  } catch (error) {
    // Silently skip files that can't be read
  }
}

function scanDirectory(dirPath) {
  try {
    const entries = readdirSync(dirPath);
    
    for (const entry of entries) {
      const fullPath = join(dirPath, entry);
      
      try {
        const stats = statSync(fullPath);
        
        if (stats.isDirectory()) {
          if (shouldScanDirectory(fullPath, entry)) {
            scanDirectory(fullPath);
          }
        } else if (stats.isFile()) {
          totalFiles++;
          if (shouldScanFile(fullPath)) {
            scanFile(fullPath);
          }
        }
      } catch (error) {
        // Silently skip entries we can't stat
      }
    }
  } catch (error) {
    // Silently skip directories we can't read
  }
}

function printResults() {
  console.log('\n=== Placeholder Scanner Results ===\n');
  console.log(`Total files: ${totalFiles}`);
  console.log(`Scanned files: ${scannedFiles}\n`);
  
  if (issues.length === 0) {
    console.log('\x1b[32m✓ No placeholder markers found!\x1b[0m');
    console.log('  Repository is clean.\n');
    return 0;
  }
  
  console.log(`\x1b[31m✗ Found ${issues.length} placeholder marker(s):\x1b[0m\n`);
  
  // Group by file
  const byFile = {};
  for (const issue of issues) {
    if (!byFile[issue.file]) {
      byFile[issue.file] = [];
    }
    byFile[issue.file].push(issue);
  }
  
  for (const [file, fileIssues] of Object.entries(byFile)) {
    console.log(`  \x1b[33m${file}\x1b[0m`);
    for (const issue of fileIssues) {
      console.log(`    \x1b[31mLine ${issue.line}:\x1b[0m ${issue.context}`);
    }
    console.log('');
  }
  
  console.log('\x1b[33mPlease remove all placeholder markers before committing.\x1b[0m\n');
  return 1;
}

function main() {
  console.log('Scanning for placeholder markers...');
  console.log(`Root: ${REPO_ROOT}\n`);
  
  scanDirectory(REPO_ROOT);
  
  const exitCode = printResults();
  process.exit(exitCode);
}

main();
