#!/usr/bin/env node

/**
 * Setup Git Hooks for Husky
 * This script configures git to use the .husky directory for hooks.
 * It's designed to be resilient and not fail npm install if git is unavailable.
 */

const { execSync } = require('child_process');
const path = require('path');
const fs = require('fs');

// Get the repository root (parent of Aura.Web)
const repoRoot = path.resolve(__dirname, '..', '..');

// Check if this is a git repository
const gitDir = path.join(repoRoot, '.git');
if (!fs.existsSync(gitDir)) {
  console.log('Not a git repository, skipping git hooks setup');
  process.exit(0);
}

// Check if .husky directory exists
const huskyDir = path.join(repoRoot, '.husky');
if (!fs.existsSync(huskyDir)) {
  console.log('.husky directory not found, skipping git hooks setup');
  process.exit(0);
}

try {
  // Try to configure git hooks path
  execSync('git config core.hooksPath .husky', {
    cwd: repoRoot,
    stdio: 'ignore'
  });
  console.log('✓ Git hooks configured successfully');
  process.exit(0);
} catch (error) {
  // If git is not available or command fails, just warn but don't fail
  console.warn('Warning: Could not configure git hooks. This is optional for building.');
  console.warn('If you plan to contribute, please ensure git is installed and in your PATH.');
  process.exit(0); // Exit with success to not fail npm install
}
