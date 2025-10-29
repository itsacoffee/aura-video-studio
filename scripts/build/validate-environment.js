#!/usr/bin/env node

/**
 * Environment validation script
 * Checks that the build environment meets all requirements
 * before attempting to build the application.
 * 
 * Validates:
 * - Node.js version matches .nvmrc exactly
 * - npm version meets minimum requirements
 * - Git configuration (Windows: long paths, line endings)
 * - FFmpeg installation (for video processing)
 * - PowerShell execution policy (Windows only)
 * - package.json engines configuration
 */

import { execSync } from 'child_process';
import { readFileSync, existsSync } from 'fs';
import { fileURLToPath } from 'url';
import { dirname, join } from 'path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const REQUIRED_NODE_VERSION = '18.18.0'; // Exact version from .nvmrc
const REQUIRED_NPM_VERSION = '9.0.0';

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

function compareVersions(version1, version2) {
  const v1 = version1.split('.').map(Number);
  const v2 = version2.split('.').map(Number);
  
  for (let i = 0; i < Math.max(v1.length, v2.length); i++) {
    const num1 = v1[i] || 0;
    const num2 = v2[i] || 0;
    
    if (num1 > num2) return 1;
    if (num1 < num2) return -1;
  }
  
  return 0;
}

function checkNodeVersion() {
  try {
    const version = process.version.replace('v', '');
    log(`Node.js version: ${version}`, 'info');
    
    // Read .nvmrc to get exact required version
    const nvmrcPath = join(__dirname, '..', '..', 'Aura.Web', '.nvmrc');
    let requiredVersion = REQUIRED_NODE_VERSION;
    
    if (existsSync(nvmrcPath)) {
      requiredVersion = readFileSync(nvmrcPath, 'utf8').trim();
      log(`.nvmrc specifies version: ${requiredVersion}`, 'info');
      
      // Check for exact version match
      if (version === requiredVersion) {
        log(`Node.js version matches .nvmrc exactly`, 'success');
        return true;
      } else {
        log(`Node.js version mismatch!`, 'error');
        log(`  Current: ${version}`, 'error');
        log(`  Required: ${requiredVersion} (from .nvmrc)`, 'error');
        log('', 'info');
        log('To fix this issue:', 'info');
        log(`  1. Install nvm: https://github.com/nvm-sh/nvm (Linux/Mac) or https://github.com/coreybutler/nvm-windows (Windows)`, 'info');
        log(`  2. Run: nvm install ${requiredVersion}`, 'info');
        log(`  3. Run: nvm use ${requiredVersion}`, 'info');
        log(`  Or download Node.js ${requiredVersion} from https://nodejs.org/`, 'info');
        hasErrors = true;
        return false;
      }
    }
    
    // Fallback to minimum version check if .nvmrc not found
    if (compareVersions(version, REQUIRED_NODE_VERSION) >= 0) {
      log('Node.js version meets minimum requirements', 'success');
      return true;
    } else {
      log(`Node.js version ${version} is below required ${REQUIRED_NODE_VERSION}`, 'error');
      log(`Please install Node.js ${REQUIRED_NODE_VERSION} or higher from https://nodejs.org/`, 'error');
      hasErrors = true;
      return false;
    }
  } catch (error) {
    log(`Failed to check Node.js version: ${error.message}`, 'error');
    hasErrors = true;
    return false;
  }
}

function checkNpmVersion() {
  try {
    const version = execSync('npm --version', { encoding: 'utf8' }).trim();
    log(`npm version: ${version}`, 'info');
    
    if (compareVersions(version, REQUIRED_NPM_VERSION) >= 0) {
      log('npm version meets requirements', 'success');
      return true;
    } else {
      log(`npm version ${version} is below required ${REQUIRED_NPM_VERSION}`, 'error');
      log(`Please update npm: npm install -g npm@latest`, 'error');
      hasErrors = true;
      return false;
    }
  } catch (error) {
    log(`Failed to check npm version: ${error.message}`, 'error');
    hasErrors = true;
    return false;
  }
}

function checkGitConfig() {
  if (process.platform !== 'win32') {
    return true;
  }
  
  try {
    const longpaths = execSync('git config --get core.longpaths', { 
      encoding: 'utf8',
      stdio: ['pipe', 'pipe', 'pipe']
    }).trim();
    
    if (longpaths !== 'true') {
      log('Git long paths support is not enabled', 'warning');
      log('Run: git config --global core.longpaths true', 'warning');
      hasWarnings = true;
    } else {
      log('Git long paths enabled', 'success');
    }
  } catch (error) {
    log('Git long paths support is not enabled', 'warning');
    log('Run: git config --global core.longpaths true', 'warning');
    hasWarnings = true;
  }
  
  try {
    const autocrlf = execSync('git config --get core.autocrlf', {
      encoding: 'utf8',
      stdio: ['pipe', 'pipe', 'pipe']
    }).trim();
    
    if (autocrlf !== 'true' && autocrlf !== 'input') {
      log('Git line ending configuration not set', 'warning');
      log('Run: git config --global core.autocrlf true', 'warning');
      hasWarnings = true;
    } else {
      log(`Git line endings configured (${autocrlf})`, 'success');
    }
  } catch (error) {
    log('Git line ending configuration not set', 'warning');
    hasWarnings = true;
  }
  
  return true;
}

function checkPackageJson() {
  try {
    const packagePath = join(__dirname, '..', '..', 'Aura.Web', 'package.json');
    const packageJson = JSON.parse(readFileSync(packagePath, 'utf8'));
    
    if (!packageJson.engines) {
      log('package.json missing "engines" field', 'warning');
      hasWarnings = true;
      return false;
    }
    
    if (!packageJson.engines.node) {
      log('package.json missing "engines.node" field', 'warning');
      hasWarnings = true;
      return false;
    }
    
    log('package.json engines configuration found', 'success');
    return true;
  } catch (error) {
    log(`Failed to read package.json: ${error.message}`, 'warning');
    hasWarnings = true;
    return false;
  }
}

function checkFFmpeg() {
  try {
    const version = execSync('ffmpeg -version', { 
      encoding: 'utf8',
      stdio: ['pipe', 'pipe', 'pipe']
    });
    
    const versionMatch = version.match(/ffmpeg version ([^\s]+)/);
    if (versionMatch) {
      log(`FFmpeg found: ${versionMatch[1]}`, 'success');
      return true;
    } else {
      log('FFmpeg found but version could not be determined', 'warning');
      hasWarnings = true;
      return true;
    }
  } catch (error) {
    log('FFmpeg not found in PATH', 'warning');
    log('FFmpeg is required for video rendering', 'warning');
    log('', 'info');
    log('To install FFmpeg:', 'info');
    if (process.platform === 'win32') {
      log('  Windows: Download from https://ffmpeg.org/download.html', 'info');
      log('  Or use winget: winget install ffmpeg', 'info');
      log('  Or use chocolatey: choco install ffmpeg', 'info');
    } else if (process.platform === 'darwin') {
      log('  macOS: brew install ffmpeg', 'info');
    } else {
      log('  Linux: sudo apt-get install ffmpeg (Ubuntu/Debian)', 'info');
      log('  Linux: sudo yum install ffmpeg (RHEL/CentOS)', 'info');
    }
    hasWarnings = true;
    return false;
  }
}

function checkPowerShellPolicy() {
  if (process.platform !== 'win32') {
    return true;
  }
  
  try {
    const policy = execSync('powershell -Command "Get-ExecutionPolicy"', { 
      encoding: 'utf8',
      stdio: ['pipe', 'pipe', 'pipe']
    }).trim();
    
    log(`PowerShell execution policy: ${policy}`, 'info');
    
    if (policy === 'Restricted') {
      log('PowerShell execution policy is Restricted', 'warning');
      log('This may prevent some build scripts from running', 'warning');
      log('', 'info');
      log('To fix (run PowerShell as Administrator):', 'info');
      log('  Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser', 'info');
      hasWarnings = true;
      return false;
    } else {
      log('PowerShell execution policy allows script execution', 'success');
      return true;
    }
  } catch (error) {
    log('Could not check PowerShell execution policy', 'warning');
    hasWarnings = true;
    return false;
  }
}

function checkHuskyInstallation() {
  try {
    const huskyPath = join(__dirname, '..', '..', '.husky');
    
    if (existsSync(huskyPath)) {
      log('Husky git hooks directory found', 'success');
      
      const preCommitPath = join(huskyPath, 'pre-commit');
      const commitMsgPath = join(huskyPath, 'commit-msg');
      
      if (existsSync(preCommitPath) && existsSync(commitMsgPath)) {
        log('Husky pre-commit and commit-msg hooks installed', 'success');
        return true;
      } else {
        log('Husky hooks incomplete', 'warning');
        log('Run: cd Aura.Web && npm run prepare', 'warning');
        hasWarnings = true;
        return false;
      }
    } else {
      log('Husky not installed', 'warning');
      log('Run: cd Aura.Web && npm run prepare', 'warning');
      hasWarnings = true;
      return false;
    }
  } catch (error) {
    log('Could not check Husky installation', 'warning');
    hasWarnings = true;
    return false;
  }
}

function main() {
  console.log('\n=== Environment Validation ===\n');
  console.log(`Platform: ${process.platform}`);
  console.log(`Architecture: ${process.arch}\n`);
  
  checkNodeVersion();
  checkNpmVersion();
  checkGitConfig();
  checkPackageJson();
  checkFFmpeg();
  checkPowerShellPolicy();
  checkHuskyInstallation();
  
  console.log('\n=== Validation Summary ===\n');
  
  if (hasErrors) {
    log('Environment validation failed with errors', 'error');
    log('Please fix the errors above before building', 'error');
    console.log('');
    process.exit(1);
  } else if (hasWarnings) {
    log('Environment validation passed with warnings', 'warning');
    log('Build can proceed, but consider addressing warnings', 'warning');
    console.log('');
    process.exit(0);
  } else {
    log('Environment validation passed', 'success');
    log('Build environment is ready', 'success');
    console.log('');
    process.exit(0);
  }
}

main();
